// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// ReSharper disable All
#pragma warning disable SA1600
namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Graph.Models;
    using Microsoft.Kiota.Abstractions.Extensions;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common;
    using Sample.Common.Beta.Logging;
    using Sample.PolicyRecordingBot.FrontEnd.Bot.Grouping;
    using RejectReason = Microsoft.Graph.Communications.Common.RejectReason;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    internal class CallHandler : HeartbeatHandler
    {
        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        private readonly IConfiguration configuration;

        // hashSet of the available sockets
        private readonly HashSet<uint> availableSocketIds = new HashSet<uint>();

        // this is an LRU cache with the MSI values, we update this Cache with the dominant speaker events
        // this way we can make sure that the muliview sockets are subscribed to the active (speaking) participants
        private readonly LRUCache currentVideoSubscriptions = new LRUCache(SampleConstants.NumberOfMultiviewSockets + 1);

        private readonly object subscriptionLock = new object();

        // This dictionnary helps maintaining a mapping of the sockets subscriptions
        private readonly ConcurrentDictionary<uint, uint> msiToSocketIdMapping = new ConcurrentDictionary<uint, uint>();

        private readonly ConcurrentDictionary<string, ChildCallHandler> childCallsHandlers = new ConcurrentDictionary<string, ChildCallHandler>();
        private readonly AudioBufferAsyncWriter audioBufferAsyncWriter;
        private int participantsCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">The stateful call.</param>
        /// <param name="configuration">The configuration.</param>
        public CallHandler(ICall statefulCall, IConfiguration configuration)
            : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger)
        {
            this.configuration = configuration;
            this.Call = statefulCall;
            this.Call.OnUpdated += this.CallOnUpdated;

            // subscribe to dominant speaker event on the audioSocket
            var audioSocket = this.Call.GetLocalMediaSession().AudioSocket;
            audioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;

            // susbscribe to the participants updates, this will inform the bot if a particpant left/joined the conference
            this.Call.Participants.OnUpdated += this.ParticipantsOnUpdated;
            this.Call.ParticipantLeftHandler += this.ParticipantLeft;
            this.Call.ParticipantJoiningHandler += this.ParticipantJoining;
            this.audioBufferAsyncWriter = new AudioBufferAsyncWriter(this.Call, this.configuration.DisableAudioStreamIo);

            // attach the botMediaStream
            this.BotMediaStream = new BotMediaStream(this.Call.GetLocalMediaSession(), this.audioBufferAsyncWriter, this.GraphLogger, this.configuration.DisableAudioStreamIo);

            // Count the first answer
            this.participantsCount = 1;
        }

        /// <summary>
        /// Gets the call.
        /// </summary>
        public ICall Call { get; }

        /// <summary>
        /// Gets the bot media stream.
        /// </summary>
        public BotMediaStream BotMediaStream { get; private set; }

        public void Initialize()
        {
            // BackingStore: Participation method [this has been called more places in our code]
            var applicationMetadata = (JsonElement)this.Call.Resource.AdditionalData["applicationMetadata"];

            // Ref - To fetch Meeting subject we need meeting Url
            var meetingUrl = this.Call.Resource.AdditionalData.ContainsKey("MeetingUrl")
                ? this.Call.Resource.AdditionalData["MeetingUrl"]?.ToString()
                : string.Empty;
        }

        /// <inheritdoc/>
        protected override Task HeartbeatAsync(ElapsedEventArgs args)
        {
            try
            {
                return this.Call.KeepAliveAsync();
            }
            catch (Exception)
            {
                // Console.WriteLine(e);
                // throw;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            var audioSocket = this.Call.GetLocalMediaSession().AudioSocket;
            audioSocket.DominantSpeakerChanged -= this.OnDominantSpeakerChanged;

            this.Call.OnUpdated -= this.CallOnUpdated;
            this.Call.Participants.OnUpdated -= this.ParticipantsOnUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            if (this.childCallsHandlers.IsEmpty)
            {
                this.BotMediaStream.Dispose();
            }
        }

        /// <summary>
        /// Called when recording status flip timer fires.
        /// </summary>
        /// <param name="recordingStatus">The status to update with.</param>
        private void OnRecordingStatusFlip(RecordingStatus recordingStatus)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // NOTE: if your implementation supports stopping the recording during the call, you can call the same method above with RecordingStatus.NotRecording
                    await this.Call
                        .UpdateRecordingStatusAsync(recordingStatus)
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    this.GraphLogger.Error(exc, $"Failed to flip the recording status to {recordingStatus}");
                }
            }).ForgetAndLogExceptionAsync(this.GraphLogger);
        }

        /// <summary>
        /// Event fired when the call has been updated.
        /// </summary>
        /// <param name="sender">The call.</param>
        /// <param name="e">The event args containing call changes.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
        {
            RequestTelemetryHelper.OnNotificationReceived(e.AdditionalData);
            if (e.OldResource.State != e.NewResource.State && e.NewResource.State == CallState.Established)
            {
                // Call is established. We should start receiving Audio, we can inform clients that we have started recording.
                this.OnRecordingStatusFlip(RecordingStatus.Recording);
            }
        }

        /// <summary>
        /// Event fired when the participants collection has been updated.
        /// </summary>
        /// <param name="sender">Participants collection.</param>
        /// <param name="args">Event args containing added and removed participants.</param>
        private void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            RequestTelemetryHelper.OnNotificationReceived(args.AdditionalData);
            this.GraphLogger.Warn($"$[{this.Call.Id}] ---NICE--- {nameof(this.ParticipantsOnUpdated)} Entry");
            StringBuilder participants = new StringBuilder();
            participants.Append("Participant Added [");

            foreach (var participant in args.AddedResources)
            {
                // todo remove the cast with the new graph implementation,
                // for now we want the bot to only subscribe to "real" participants
                var participantDetails = participant.Resource.Info.Identity.User;

                // BackingStore: property access
                this.GetParticipantHolder(participant);

                if (participantDetails != null)
                {
                    participants.Append($"{participantDetails.Id}, ");

                    // subscribe to the participant updates, this will indicate if the user started to share,
                    // or added another modality
                    participant.OnUpdated += this.OnParticipantUpdated;
                    foreach (var childCallHandler in this.childCallsHandlers.Values)
                    {
                        childCallHandler.SubscribeForParticipantUpdate(participant);
                    }

                    // the behavior here is to avoid subscribing to a new participant video if the VideoSubscription cache is full
                    this.SubscribeToParticipantVideo(participant, forceSubscribe: false);
                }
            }

            participants.Append("]");
            participants.Append("Participant Removed [");
            foreach (var participant in args.RemovedResources)
            {
                var participantDetails = participant.Resource.Info.Identity.User;

                // BackingStore: property access
                this.GetParticipantHolder(participant);
                if (participantDetails != null)
                {
                    participants.Append($"{participantDetails.Id}, ");

                    // unsubscribe to the participant updates
                    participant.OnUpdated -= this.OnParticipantUpdated;
                    if (this.childCallsHandlers.TryGetValue(participant.Id, out var childCallHandler))
                    {
                        childCallHandler.UnSubscribeForParticipantUpdate(participant);
                    }

                    this.UnsubscribeFromParticipantVideo(participant);
                }
            }

            participants.Append("]");
            this.GraphLogger.Warn($"{participants}");
        }

        private void GetParticipantHolder(IParticipant participant)
        {
            this.GraphLogger.Warn($"[{this.Call.Id}]: Participant Holder -> getting participant info");
            var participantInfo = participant.Resource.Info;
            var identitySet = participantInfo.Identity;
            var identityWithType = identitySet.GetPrimaryIdentityWithType_NICE(); // to decide is participant is a bot
            var endpointType = participant.Resource.Info.EndpointType;
            List<uint> streamIds = new List<uint>();
            streamIds = streamIds.Union(participant.Resource.MediaStreams
                    .Where(_ => _.MediaType != Modality.Data &&
                                _.SourceId != null) // to handle removed participants in delta roster
                    .Select(_ => uint.Parse(_.SourceId)))
                .ToList();
        }

        /// <summary>
        /// Event fired when a participant associated to the bot has left.
        /// </summary>
        /// <param name="call">The call object contains details of the participant left.</param>
        /// <param name="participantId">The id of the participant that left.</param>
        private void ParticipantLeft(Call call, string participantId)
        {
            this.participantsCount--;
            if (this.childCallsHandlers.TryRemove(participantId, out var callHandler))
            {
                callHandler.UnSubscribeForParticipantsCollectionUpdate();
                callHandler.Dispose();
                this.GraphLogger.Warn($"[{this.Call.Id}:The participant {participantId} has left with code {call?.ResultInfo?.Code} and subcode {call?.ResultInfo?.Subcode})");
            }
        }

        /// <summary>
        /// Event fired when a participant associated to the bot has join in same group.
        /// </summary>
        /// <param name="call">The call object contains details of the participant joining.</param>
        /// <returns>The response to participant joining notification.</returns>
        private ParticipantJoiningResponse ParticipantJoining(Call call)
        {
            if (this.participantsCount < this.configuration.GroupSize)
            {
                // BackingStore property access
                this.GraphLogger.Warn($"ParticipantJoining called for user {call.Source?.Identity?.User?.Id}");
                this.participantsCount++;
                var childCall = new ChildCallHandler(this.Call, call, this.configuration);
                childCall.Register();
                call.Source.AdditionalData.TryGetValue("id", out var participantId);
                this.childCallsHandlers.TryAdd(participantId.ToString(), childCall);
                childCall.SubscribeForParticipantsCollectionUpdate();
                return new AcceptJoinResponse();
            }
            else
            {
                return new RejectJoinResponse()
                {
                    Reason = RejectReason.Busy,
                };

                /* Use InviteNewBotResponse with url to redirect to another bot instance.
                return new InviteNewBotResponse()
                {
                    InviteUri = "https://redirect.uri",
                }
                */
            }
        }

        /// <summary>
        /// Event fired when a participant is updated.
        /// </summary>
        /// <param name="sender">Participant object.</param>
        /// <param name="args">Event args containing the old values and the new values.</param>
        private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            this.SubscribeToParticipantVideo(sender, forceSubscribe: false);
        }

        /// <summary>
        /// Unsubscribe and free up the video socket for the specified participant.
        /// </summary>
        /// <param name="participant">Particant to unsubscribe the video.</param>
        private void UnsubscribeFromParticipantVideo(IParticipant participant)
        {
            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
              (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();

            if (participantSendCapableVideoStream != null)
            {
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (this.subscriptionLock)
                {
                    if (this.currentVideoSubscriptions.TryRemove(msi))
                    {
                        if (this.msiToSocketIdMapping.TryRemove(msi, out uint socketId))
                        {
                            this.BotMediaStream.Unsubscribe(MediaType.Video, socketId);
                            this.availableSocketIds.Add(socketId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Subscribe to video or vbss sharer.
        /// if we set the flag forceSubscribe to true, the behavior is to subscribe to a video even if there is no available socket left.
        /// in that case we use the LRU cache to free socket and subscribe to the new MSI.
        /// </summary>
        /// <param name="participant">Participant sending the video or VBSS stream.</param>
        /// <param name="forceSubscribe">If forced, the least recently used video socket is released if no sockets are available.</param>
        private void SubscribeToParticipantVideo(IParticipant participant, bool forceSubscribe = true)
        {
            bool subscribeToVideo = false;
            uint socketId = uint.MaxValue;

            // filter the mediaStreams to see if the participant has a video send
            var participantSendCapableVideoStream = participant.Resource.MediaStreams.Where(x => x.MediaType == Modality.Video &&
               (x.Direction == MediaDirection.SendReceive || x.Direction == MediaDirection.SendOnly)).FirstOrDefault();
            if (participantSendCapableVideoStream != null)
            {
                bool updateMSICache = false;
                var msi = uint.Parse(participantSendCapableVideoStream.SourceId);
                lock (this.subscriptionLock)
                {
                    if (this.currentVideoSubscriptions.Count < this.Call.GetLocalMediaSession().VideoSockets.Count)
                    {
                        // we want to verify if we already have a socket subscribed to the MSI
                        if (!this.msiToSocketIdMapping.ContainsKey(msi))
                        {
                            if (this.availableSocketIds.Any())
                            {
                                socketId = this.availableSocketIds.Last();
                                this.availableSocketIds.Remove((uint)socketId);
                                subscribeToVideo = true;
                            }
                        }

                        updateMSICache = true;
                        this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(socket {socketId} available, the number of remaining sockets is {this.availableSocketIds.Count}, subscribing to the participant {participant.Id})");
                    }
                    else if (forceSubscribe)
                    {
                        // here we know that all the sockets subscribed to a video we need to update the msi cache,
                        // and obtain the socketId to reuse with the new MSI
                        updateMSICache = true;
                        subscribeToVideo = true;
                    }

                    if (updateMSICache)
                    {
                        this.currentVideoSubscriptions.TryInsert(msi, out uint? dequeuedMSIValue);
                        if (dequeuedMSIValue != null)
                        {
                            // Cache was updated, we need to use the new available socket to subscribe to the MSI
                            this.msiToSocketIdMapping.TryRemove((uint)dequeuedMSIValue, out socketId);
                        }
                    }
                }

                if (subscribeToVideo && socketId != uint.MaxValue)
                {
                    this.msiToSocketIdMapping.AddOrUpdate(msi, socketId, (k, v) => socketId);

                    this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the participant {participant.Id} on socket {socketId})");
                    this.BotMediaStream.Subscribe(MediaType.Video, msi, VideoResolution.HD1080p, socketId);
                }
            }

            // vbss viewer subscription
            var vbssParticipant = participant.Resource.MediaStreams.SingleOrDefault(x => x.MediaType == Modality.VideoBasedScreenSharing
            && x.Direction == MediaDirection.SendOnly);
            if (vbssParticipant != null)
            {
                // new sharer
                this.GraphLogger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the VBSS sharer {participant.Id})");
                this.BotMediaStream.Subscribe(MediaType.Vbss, uint.Parse(vbssParticipant.SourceId), VideoResolution.HD1080p, socketId);
            }
        }

        /// <summary>
        /// Listen for dominant speaker changes in the conference.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The dominant speaker changed event arguments.
        /// </param>
        private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
        {
            this.GraphLogger.Info($"[{this.Call.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]");

            if (e.CurrentDominantSpeaker != DominantSpeakerNone)
            {
                IParticipant participant = this.GetParticipantFromMSI(e.CurrentDominantSpeaker);
                var participantDetails = participant?.Resource?.Info?.Identity?.User;
                if (participantDetails != null)
                {
                    // we want to force the video subscription on dominant speaker events
                    this.SubscribeToParticipantVideo(participant, forceSubscribe: true);
                }
            }
        }

        /// <summary>
        /// Gets the participant with the corresponding MSI.
        /// </summary>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="IParticipant"/>.
        /// </returns>
        private IParticipant GetParticipantFromMSI(uint msi)
        {
            return this.Call.Participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }
    }
}
