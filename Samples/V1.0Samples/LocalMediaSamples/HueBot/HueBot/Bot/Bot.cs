// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Fabric;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Client;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common;
    using Sample.Common.Authentication;
    using Sample.Common.Meetings;
    using Sample.Common.OnlineMeetings;
    using Sample.HueBot.Controllers;
    using Sample.HueBot.Extensions;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    public class Bot
    {
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        private readonly IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bot"/> class.
        /// </summary>
        /// <param name="options">The bot options.</param>
        /// <param name="graphLogger">The graph logger.</param>
        /// <param name="serviceContext">Service context.</param>
        public Bot(BotOptions options, IGraphLogger graphLogger, StatelessServiceContext serviceContext)
        {
            this.Options = options;
            this.logger = graphLogger;

            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                options.AppId,
                this.logger);

            var authProvider = new AuthenticationProvider(
                name,
                options.AppId,
                options.AppSecret,
                this.logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(options.BotBaseUrl.ReplacePort(options.BotBaseUrl.Port + serviceContext.NodeInstance()));
            builder.SetMediaPlatformSettings(this.MediaInit(options, serviceContext));
            builder.SetServiceBaseUrl(options.PlaceCallEndpointUrl);

            this.Client = builder.Build();

            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;

            this.OnlineMeetings = new OnlineMeetingHelper(authProvider, options.PlaceCallEndpointUrl);
        }

        /// <summary>
        /// Gets the configuration for the bot.
        /// </summary>
        public BotOptions Options { get; }

        /// <summary>
        /// Gets the call handlers.
        /// </summary>
        /// <value>
        /// The call handlers.
        /// </value>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ICommunicationsClient Client { get; }

        /// <summary>
        /// Gets the online meeting.
        /// </summary>
        /// <value>
        /// The online meeting.
        /// </value>
        public OnlineMeetingHelper OnlineMeetings { get; }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall"/> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallController.JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
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
                //// meetingInfo.AllowConversationWithoutHost = joinCallBody.AllowConversationWithoutHost;
            }
            else
            {
                (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);
            }

            var tenantId =
                joinCallBody.TenantId ??
                (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId();
            ILocalMediaSession mediaSession = this.CreateLocalMediaSession();

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
            this.logger.Info($"Call creation complete: {statefulCall.Id}");
            return statefulCall;
        }

        /// <summary>
        /// Get the image for a particular call.
        /// </summary>
        /// <param name="callId">
        /// The thread Id.
        /// </param>
        /// <returns>
        /// The screenshot data.
        /// </returns>
        internal Bitmap GetScreenshotByCallId(string callId)
        {
            return this.GetHandlerOrThrow(callId).LatestScreenshotImage;
        }

        /// <summary>
        /// Get the video hue for a particular call.
        /// </summary>
        /// <param name="callId">
        /// The thread Id.
        /// </param>
        /// <returns>Current hue.</returns>
        internal string GetVideoHueByCallId(string callId)
        {
            return this.GetHandlerOrThrow(callId).GetHue();
        }

        /// <summary>
        /// Change the video hue for a particular call.
        /// </summary>
        /// <param name="callId">
        /// The thread Id.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        internal void SetVideoHueByCallId(string callId, string color)
        {
            this.GetHandlerOrThrow(callId).SetHue(color);
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task EndCallByCallIdAsync(string callId)
        {
            await this.GetHandlerOrThrow(callId).Call.DeleteAsync().ConfigureAwait(false);
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
                this.CallHandlers.GetOrAdd(call.Id, new CallHandler(call));
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
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">
        /// The media session identifier.
        /// This should be a unique value for each call.
        /// </param>
        /// <returns>The <see cref="ILocalMediaSession"/>.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default(Guid))
        {
            var mediaSession = this.Client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    SupportedAudioFormat = AudioFormat.Pcm16K,
                },
                new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    ReceiveColorFormat = VideoColorFormat.NV12,

                    // We loop back the video in this sample. The MediaPlatform always sends only NV12 frames. So include only NV12 video in supportedSendVideoFormats
                    SupportedSendVideoFormats = new List<VideoFormat>
                    {
                        VideoFormat.NV12_270x480_15Fps,
                        VideoFormat.NV12_320x180_15Fps,
                        VideoFormat.NV12_360x640_15Fps,
                        VideoFormat.NV12_424x240_15Fps,
                        VideoFormat.NV12_480x270_15Fps,
                        VideoFormat.NV12_480x848_30Fps,
                        VideoFormat.NV12_640x360_15Fps,
                        VideoFormat.NV12_720x1280_30Fps,
                        VideoFormat.NV12_848x480_30Fps,
                        VideoFormat.NV12_960x540_30Fps,
                        VideoFormat.NV12_1280x720_30Fps,
                        VideoFormat.NV12_1920x1080_30Fps,
                    },
                },
                mediaSessionId: mediaSessionId);
            return mediaSession;
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="callId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="CallHandler"/>.
        /// </returns>
        private CallHandler GetHandlerOrThrow(string callId)
        {
            if (!this.CallHandlers.TryGetValue(callId, out CallHandler handler))
            {
                throw new Exception($"call ({callId}) not found");
            }

            return handler;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HueBot"/> class.
        /// </summary>
        /// <param name="options">The bot options.</param>
        /// <param name="serviceContext">Service context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private MediaPlatformSettings MediaInit(BotOptions options, StatelessServiceContext serviceContext)
        {
            var instanceNumber = serviceContext.NodeInstance();
            var publicMediaUrl = options.BotMediaProcessorUrl ?? options.BotBaseUrl;

            var instanceAddresses = Dns.GetHostEntry(publicMediaUrl.Host).AddressList;
            if (instanceAddresses.Length == 0)
            {
                throw new InvalidOperationException("Could not resolve the PIP hostname. Please make sure that PIP is properly configured for the service");
            }

            return new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = options.Certificate,
                    InstanceInternalPort = serviceContext.CodePackageActivationContext.GetEndpoint("MediaPort").Port,
                    InstancePublicIPAddress = instanceAddresses[0],
                    InstancePublicPort = publicMediaUrl.Port + instanceNumber,
                    ServiceFqdn = publicMediaUrl.Host,
                },
                ApplicationId = options.AppId,
            };
        }
    }
}
