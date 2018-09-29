// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph;
    using Microsoft.Graph.Calls;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Graph.Core.Telemetry;
    using Microsoft.Graph.CoreSDK.Serialization;
    using Microsoft.Graph.StatefulClient;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common.Logging;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    internal class CallHandler : IDisposable
    {
        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        private static readonly Serializer Serializer = new Serializer(pretty: true);

        // hashSet of the available sockets
        private readonly HashSet<uint> availableSocketIds = new HashSet<uint>();

        // this is an LRU cache with the MSI values, we update this Cache with the dominant speaker events
        // this way we can make sure that the muliview sockets are subscribed to the active (speaking) participants
        private readonly LRUCache currentVideoSubscriptions = new LRUCache(Constants.NumberOfMultivewSockets + 1);

        private readonly object subscriptionLock = new object();

        // This dictionnary helps maintaining a mapping of the sockets subscriptions
        private readonly ConcurrentDictionary<uint, uint> msiToSocketIdMapping = new ConcurrentDictionary<uint, uint>();

        // Graph logger.
        private readonly IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">The stateful call.</param>
        /// <param name="logger">Logger instance.</param>
        public CallHandler(ICall statefulCall, IGraphLogger logger)
        {
            this.Call = statefulCall;
            this.logger = logger;

            // subscribe to call updates
            this.Call.OnUpdated += this.CallOnUpdated;

            // subscribe to dominant speaker event on the audioSocket
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;

            // subscribe to the VideoMediaReceived event on the main video socket
            this.Call.GetLocalMediaSession().VideoSockets.FirstOrDefault().VideoMediaReceived += this.OnVideoMediaReceived;

            // susbscribe to the participants updates, this will inform the bot if a particpant left/joined the conference
            this.Call.Participants.OnUpdated += this.ParticipantsOnUpdated;

            foreach (var videoSocket in this.Call.GetLocalMediaSession().VideoSockets)
            {
                this.availableSocketIds.Add((uint)videoSocket.SocketId);
            }

            var outcome = Serializer.SerializeObject(statefulCall.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Created:\n" + outcome);

            // attach the botMediaStream
            this.BotMediaStream = new BotMediaStream(this.Call.GetLocalMediaSession(), this.logger);
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
        /// Gets the outcomes log - maintained for easy checking of async server responses.
        /// </summary>
        /// <value>
        /// The outcomes log.
        /// </value>
        public LinkedList<string> OutcomesLogMostRecentFirst { get; } = new LinkedList<string>();

        /// <inheritdoc />
        public void Dispose()
        {
            this.Call.OnUpdated -= this.CallOnUpdated;
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged -= this.OnDominantSpeakerChanged;
            this.Call.GetLocalMediaSession().VideoSockets.FirstOrDefault().VideoMediaReceived -= this.OnVideoMediaReceived;

            this.Call.Participants.OnUpdated -= this.ParticipantsOnUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.BotMediaStream?.ShutdownAsync().ForgetAndLogExceptionAsync(this.logger);
        }

        /// <summary>
        /// Event fired when call has been updated.
        /// </summary>
        /// <param name="sender">Call object.</param>
        /// <param name="args">Event args containing the old values and the new values.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Updated:\n" + outcome);
        }

        /// <summary>
        /// Event fired when the participants collection has been updated.
        /// </summary>
        /// <param name="sender">Participants collection.</param>
        /// <param name="args">Event args containing added and removed participants.</param>
        private void ParticipantsOnUpdated(ICallParticipantCollection sender, CollectionEventArgs<ICallParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Added:\n" + outcome);

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

                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Removed:\n" + outcome);
            }
        }

        /// <summary>
        /// Event fired when a participant is updated.
        /// </summary>
        /// <param name="sender">Participant object.</param>
        /// <param name="args">Event args containing the old values and the new values.</param>
        private void OnParticipantUpdated(ICallParticipant sender, ResourceEventArgs<Participant> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Participant Updated:\n" + outcome);

            this.SubscribeToParticipantVideo(sender, forceSubscribe: false);
        }

        /// <summary>
        /// Unsubscribe and free up the video socket for the specified participant.
        /// </summary>
        /// <param name="participant">Particant to unsubscribe the video.</param>
        private void UnsubscribeFromParticipantVideo(ICallParticipant participant)
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
        private void SubscribeToParticipantVideo(ICallParticipant participant, bool forceSubscribe = true)
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
                        this.logger.Info($"[{this.Call.Id}:SubscribeToParticipant(socket {socketId} available, the number of remaining sockets is {this.availableSocketIds.Count}, subscribing to the participant {participant.Id})");
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

                    this.logger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the participant {participant.Id} on socket {socketId})");
                    this.BotMediaStream.Subscribe(MediaType.Video, msi, VideoResolution.HD1080p, socketId);
                }
            }

            // vbss viewer subscription
            var vbssParticipant = participant.Resource.MediaStreams.SingleOrDefault(x => x.MediaType == Modality.VideoBasedScreenSharing
            && x.Direction == MediaDirection.SendOnly);
            if (vbssParticipant != null)
            {
                // new sharer
                this.logger.Info($"[{this.Call.Id}:SubscribeToParticipant(subscribing to the VBSS sharer {participant.Id})");
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
            this.logger.Info($"[{this.Call.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]");

            if (e.CurrentDominantSpeaker != DominantSpeakerNone)
            {
                ICallParticipant participant = this.GetParticipantFromMSI(e.CurrentDominantSpeaker);
                var participantDetails = participant?.Resource?.Info?.Identity?.User;
                if (participantDetails != null)
                {
                    // we want to force the video subscription on dominant speaker events
                    this.SubscribeToParticipantVideo(participant, forceSubscribe: true);
                }
            }
        }

        /// <summary>
        /// Save screenshots when we receive video from subscribed participant.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The video media received arguments.
        /// </param>
        private void OnVideoMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            // leave only logging in here
            this.logger.Info($"[{this.Call.Id}]: Capturing image: [VideoMediaReceivedEventArgs(Data=<{e.Buffer.Data.ToString()}>, Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp}, Width={e.Buffer.VideoFormat.Width}, Height={e.Buffer.VideoFormat.Height}, ColorFormat={e.Buffer.VideoFormat.VideoColorFormat}, FrameRate={e.Buffer.VideoFormat.FrameRate})]");

            e.Buffer.Dispose();
        }

        /// <summary>
        /// Gets the participant with the corresponding MSI.
        /// </summary>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="ICallParticipant"/>.
        /// </returns>
        private ICallParticipant GetParticipantFromMSI(uint msi)
        {
            return this.Call.Participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }
    }
}
