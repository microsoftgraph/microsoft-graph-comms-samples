// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Calls;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Graph.Core.Common;
    using Microsoft.Graph.Core.Telemetry;
    using Microsoft.Graph.Meetings;
    using Microsoft.Graph.StatefulClient;
    using Microsoft.Skype.Bots.Media;
    using Sample.AudioVideoPlaybackBot.FrontEnd;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
    using Sample.Common.Authentication;
    using Sample.Common.Logging;
    using CallerInfo = Sample.Common.Logging.CallerInfo;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    internal class Bot
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Bot"/> class from being created.
        /// </summary>
        private Bot()
        {
            var logger = new GraphLogger(nameof(Bot));
            var builder = new StatefulClientBuilder("AudioVideoPlaybackBot", Service.Instance.Configuration.AadAppId, logger);
            builder.SetAuthenticationProvider(
                new AuthenticationProvider(
                    Service.Instance.Configuration.AadAppId,
                    Service.Instance.Configuration.AadAppSecret,
                    logger));
            builder.SetNotificationUrl(Service.Instance.Configuration.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(Service.Instance.Configuration.MediaPlatformSettings);
            builder.SetServiceBaseUrl(Service.Instance.Configuration.PlaceCallEndpointUrl);

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;
        }

        /// <summary>
        /// Gets the instance of the bot.
        /// </summary>
        public static Bot Instance { get; } = new Bot();

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        public IStatefulClient Client { get; }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall"/> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallController.JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes.  Helps identify this call in logs.
            var correlationId = Guid.NewGuid();

            MeetingInfo meetingInfo = joinCallBody.MeetingInfo;
            ChatInfo chatInfo = joinCallBody.ChatInfo;
            if (!string.IsNullOrWhiteSpace(joinCallBody.MeetingId))
            {
                var onlineMeeting = await this.Client.Meetings()
                    .GetAsync(joinCallBody.MeetingId, joinCallBody.TenantId, correlationId)
                    .ConfigureAwait(false);

                meetingInfo = onlineMeeting.MeetingInfo;
                meetingInfo.AllowConversationWithoutHost = joinCallBody.MeetingInfo?.AllowConversationWithoutHost;
                chatInfo = onlineMeeting.ChatInfo;
            }

            var mediaSession = this.CreateLocalMediaSession(correlationId);

            var joinCallParameters = new JoinMeetingParameters(
                chatInfo,
                meetingInfo,
                mediaSession)
            {
                TenantId = joinCallBody.TenantId,
                CorrelationId = correlationId,
            };

            if (!string.IsNullOrWhiteSpace(joinCallBody.DisplayName))
            {
                // Teams client does not allow changing of ones own display name.
                // If display name is specified, we join as anonymous (guest) user
                // with the specified display name.  This will put bot into lobby
                // unless lobby bypass is disabled.
                joinCallParameters.GuestIdentity = new Identity
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = joinCallBody.DisplayName,
                };
            }

            var statefulCall = await this.Client.Calls().AddAsync(joinCallParameters).ConfigureAwait(false);
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Call creation complete: {statefulCall.Id}");
            return statefulCall;
        }

        /// <summary>
        /// Changes bot's screen sharing role async.
        /// </summary>
        /// <param name="callLegId">which call to change role on.</param>
        /// <param name="role">The role to change to.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ChangeSharingRoleAsync(string callLegId, ScreenSharingRole role)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId]
                .ChangeScreenSharingRoleAsync(role)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get the logs for a particular call.
        /// </summary>
        /// <param name="callLegId">
        /// The call Leg Id.
        /// </param>
        /// <param name="limit">
        /// The limit.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{String}"/>.
        /// </returns>
        internal IEnumerable<string> GetLogsByCallLegId(string callLegId, int limit)
        {
            return this.GetHandlerOrThrow(callLegId).OutcomesLogMostRecentFirst.Take(limit);
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall call);
            }
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>The <see cref="ILocalMediaSession"/>.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid correlationId)
        {
            var videoSocketSettings = new List<VideoSocketSettings>
            {
                // add the main video socket sendrecv capable
                new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    ReceiveColorFormat = VideoColorFormat.H264,

                    // We loop back the video in this sample. The MediaPlatform always sends only NV12 frames.
                    // So include only NV12 video in supportedSendVideoFormats
                    SupportedSendVideoFormats = Constants.SupportedSendVideoFormats,

                    MaxConcurrentSendStreams = 1,
                },
            };

            // create the receive only sockets settings for the multiview support
            for (int i = 0; i < Constants.NumberOfMultivewSockets; i++)
            {
                videoSocketSettings.Add(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.H264,
                });
            }

            // Create the VBSS socket settings
            var vbssSocketSettings = new VideoSocketSettings
            {
                StreamDirections = StreamDirection.Recvonly,
                ReceiveColorFormat = VideoColorFormat.NV12,
                MediaType = MediaType.Vbss,
                SupportedSendVideoFormats = new List<VideoFormat>
                {
                    // fps 1.875 is required for h264 in vbss scenario:
                    // refer to Raw/Encoded Frame Format Recommendation - VbSS section in
                    // http://msrtc/documentation/cloud_video_interop/#platform-capabilities-for-encodedecode
                    VideoFormat.H264_320x180_1_875Fps,
                },
            };

            // create media session object, this is needed to establish call connectionS
            var mediaSession = this.Client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    SupportedAudioFormat = AudioFormat.Pcm16K,
                },
                videoSocketSettings,
                vbssSocketSettings,
                correlationId);
            return mediaSession;
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}"/> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // Answer call and start video playback
                var mediaSession = this.CreateLocalMediaSession(call?.CorrelationId ?? Guid.Empty);
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync($"Answering call {call.Id} with correlation {call.CorrelationId}.");
            });
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection"/> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}"/> instance containing the event data.</param>
        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(call);
                this.CallHandlers[call.Id] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="CallHandler"/>.
        /// </returns>
        /// <exception cref="ObjectNotFoundException">
        /// Throws an exception if handler is not found.
        /// </exception>
        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!this.CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ObjectNotFoundException($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}