// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PsiBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Psi;
    using Microsoft.Psi.TeamsBot;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    public class CallHandler : HeartbeatHandler
    {
        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        // hashSet of the available sockets
        private readonly HashSet<uint> availableSocketIds = new HashSet<uint>();

        // this is an LRU cache with the MSI values, we update this Cache with the dominant speaker events
        // this way we can make sure that the muliview sockets are subscribed to the active (speaking) participants
        private readonly LRUCache currentVideoSubscriptions = new LRUCache(Bot.NumberOfMultiviewSockets + 1);

        private readonly object subscriptionLock = new object();

        // This dictionary helps maintaining a mapping of the sockets subscriptions
        private readonly ConcurrentDictionary<uint, uint> msiToSocketIdMapping = new ConcurrentDictionary<uint, uint>();

        private readonly Pipeline pipeline;
        private readonly ITeamsBot teamsBot;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">The stateful call.</param>
        public CallHandler(ICall statefulCall)
            : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger)
        {
            this.pipeline = Pipeline.Create(enableDiagnostics: true);
            this.teamsBot = CreateTeamsBot(this.pipeline);
            var exporter = PsiStore.Create(this.pipeline, $"CallStore_{statefulCall.Id}", @"C:\Psi");
            this.pipeline.Diagnostics.Write("Diagnostics", exporter);

            this.Call = statefulCall;
            this.Call.OnUpdated += this.CallOnUpdated;

            // subscribe to dominant speaker event on the audioSocket
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;

            // susbscribe to the participants updates, this will inform the bot if a particpant left/joined the meeting
            this.Call.Participants.OnUpdated += this.ParticipantsOnUpdated;

            foreach (var videoSocket in this.Call.GetLocalMediaSession().VideoSockets)
            {
                this.availableSocketIds.Add((uint)videoSocket.SocketId);
            }

            var waitingToShare = true;
            this.Call.OnUpdated += (call, args) =>
            {
                if (waitingToShare && call.Resource.State == CallState.Established && this.teamsBot.EnableScreenSharing)
                {
                    // enable screen sharing
                    this.Call.ChangeScreenSharingRoleAsync(ScreenSharingRole.Sharer).Wait();
                    waitingToShare = false;
                }
            };

            // attach the botMediaStream
            this.BotMediaStream = new BotMediaStream(this.Call.GetLocalMediaSession(), this, this.pipeline, this.teamsBot, exporter, this.GraphLogger);

            this.pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                this.GraphLogger.Error($"PSI PIPELINE ERROR: {ex.Exception.Message}");
            };
            this.pipeline.RunAsync();
        }

        /// <summary>
        /// Gets the call.
        /// </summary>
        public ICall Call { get; }

        /// <summary>
        /// Gets the bot media stream.
        /// </summary>
        public BotMediaStream BotMediaStream { get; private set; }

        /// <summary>
        /// Gets the participant with the corresponding MSI.
        /// </summary>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="IParticipant"/>.
        /// </returns>
        public IParticipant GetParticipantFromMSI(uint msi)
        {
            return this.Call.Participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }

        /// <inheritdoc/>
        protected override Task HeartbeatAsync(ElapsedEventArgs args)
        {
            return this.Call.KeepAliveAsync();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.pipeline.Dispose();
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged -= this.OnDominantSpeakerChanged;
            this.Call.OnUpdated -= this.CallOnUpdated;
            this.Call.Participants.OnUpdated -= this.ParticipantsOnUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.BotMediaStream?.ShutdownAsync().ForgetAndLogExceptionAsync(this.GraphLogger);
        }

        /// <summary>
        /// Create your ITeamsBot implementation.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>ITeamsBot instance.</returns>
        private static ITeamsBot CreateTeamsBot(Pipeline pipeline)
        {
            return new ParticipantEngagementBallBot(pipeline, TimeSpan.FromSeconds(1.0 / 15.0), 1920, 1080);
        }

        /// <summary>
        /// Event fired when the call has been updated.
        /// </summary>
        /// <param name="sender">The call.</param>
        /// <param name="e">The event args containing call changes.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
        {
            if (e.OldResource.State != e.NewResource.State && e.NewResource.State == CallState.Established)
            {
                // Call is established... do some work.
            }
        }

        /// <summary>
        /// Event fired when the participants collection has been updated.
        /// </summary>
        /// <param name="sender">Participants collection.</param>
        /// <param name="args">Event args containing added and removed participants.</param>
        private void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                // todo remove the cast with the new graph implementation,
                // for now we want the bot to only subscribe to "real" participants
                var participantDetails = participant.Resource.Info.Identity.User;
                if (participantDetails != null)
                {
                    // subscribe to the participant updates, this will indicate if the user started to share,
                    // or added another modality
                    participant.OnUpdated += this.OnParticipantUpdated;

                    // the behavior here is to avoid subscribing to a new participant video if the VideoSubscription cache is full
                    this.SubscribeToParticipantVideo(participant, forceSubscribe: false);
                }
            }

            foreach (var participant in args.RemovedResources)
            {
                var participantDetails = participant.Resource.Info.Identity.User;
                if (participantDetails != null)
                {
                    // unsubscribe to the participant updates
                    participant.OnUpdated -= this.OnParticipantUpdated;
                    this.UnsubscribeFromParticipantVideo(participant);
                }
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
    }
}
