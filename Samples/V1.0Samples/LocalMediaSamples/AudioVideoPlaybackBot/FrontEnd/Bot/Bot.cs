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
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Client;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Skype.Bots.Media;
    using Sample.AudioVideoPlaybackBot.FrontEnd;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
    using Sample.Common;
    using Sample.Common.Authentication;
    using Sample.Common.Logging;
    using Sample.Common.Meetings;
    using Sample.Common.OnlineMeetings;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    internal class Bot : IDisposable
    {
        /// <summary>
        /// Gets the instance of the bot.
        /// </summary>
        public static Bot Instance { get; } = new Bot();

        /// <summary>
        /// Gets the Graph Logger instance.
        /// </summary>
        public IGraphLogger Logger { get; private set; }

        /// <summary>
        /// Gets the sample log observer.
        /// </summary>
        public SampleObserver Observer { get; private set; }

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        public ICommunicationsClient Client { get; private set; }

        /// <summary>
        /// Gets the online meeting.
        /// </summary>
        /// <value>
        /// The online meeting.
        /// </value>
        public OnlineMeetingHelper OnlineMeetings { get; private set; }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall"/> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallController.JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes.  Helps identify this call in logs.
            var scenarioId = Guid.NewGuid();

            MeetingInfo meetingInfo;
            ChatInfo chatInfo;
            if (!string.IsNullOrWhiteSpace(joinCallBody.MeetingId))
            {
                // Meeting id is a cloud-video-interop numeric meeting id.
                var onlineMeeting = await this.OnlineMeetings
                    .GetOnlineMeetingAsync(joinCallBody.TenantId, joinCallBody.MeetingId, scenarioId)
                    .ConfigureAwait(false);

                meetingInfo = new OrganizerMeetingInfo { Organizer = onlineMeeting.Participants.Organizer.Identity, };
                chatInfo = onlineMeeting.ChatInfo;
            }
            else
            {
                (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);
            }

            var tenantId =
                joinCallBody.TenantId ??
                (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId();
            var mediaSession = this.CreateLocalMediaSession();

            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession)
            {
                TenantId = tenantId,
            };

            if (!string.IsNullOrWhiteSpace(joinCallBody.DisplayName))
            {
                // Teams client does not allow changing of ones own display name.
                // If display name is specified, we join as anonymous (guest) user
                // with the specified display name.  This will put bot into lobby
                // unless lobby bypass is disabled.
                joinParams.GuestIdentity = new Identity
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = joinCallBody.DisplayName,
                };
            }

            var statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
            statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
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

        /// <inheritdoc />
        public void Dispose()
        {
            this.Observer?.Dispose();
            this.Observer = null;
            this.Logger = null;
            this.Client?.Dispose();
            this.Client = null;
            this.OnlineMeetings = null;
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        /// <param name="service">Service instance.</param>
        /// <param name="logger">Graph logger.</param>
        internal void Initialize(Service service, IGraphLogger logger)
        {
            Validator.IsNull(this.Logger, "Multiple initializations are not allowed.");

            this.Logger = logger;
            this.Observer = new SampleObserver(logger);

            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                service.Configuration.AadAppId,
                this.Logger);

            var authProvider = new AuthenticationProvider(
                name,
                service.Configuration.AadAppId,
                service.Configuration.AadAppSecret,
                this.Logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(service.Configuration.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(service.Configuration.MediaPlatformSettings);
            builder.SetServiceBaseUrl(service.Configuration.PlaceCallEndpointUrl);

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;

            this.OnlineMeetings = new OnlineMeetingHelper(authProvider, service.Configuration.PlaceCallEndpointUrl);
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
        internal async Task<bool> EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall call);
            }

            return false;
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">
        /// The media session identifier.
        /// This should be a unique value for each call.
        /// </param>
        /// <returns>The <see cref="ILocalMediaSession"/>.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default(Guid))
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
                    SupportedSendVideoFormats = SampleConstants.SupportedSendVideoFormats,

                    MaxConcurrentSendStreams = 1,
                },
            };

            // create the receive only sockets settings for the multiview support
            for (int i = 0; i < SampleConstants.NumberOfMultiviewSockets; i++)
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
                ReceiveColorFormat = VideoColorFormat.H264,
                MediaType = MediaType.Vbss,
                SupportedSendVideoFormats = new List<VideoFormat>
                {
                    // fps 1.875 is required for h264 in vbss scenario.
                    VideoFormat.H264_1920x1080_1_875Fps,
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
                mediaSessionId: mediaSessionId);
            return mediaSession;
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TEntity}"/> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call and start video playback
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
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