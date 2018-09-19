// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.TestCallingBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Calls;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Graph.Core.Telemetry;
    using Microsoft.Graph.CoreSDK.Exceptions;
    using Microsoft.Graph.Meetings;
    using Microsoft.Graph.StatefulClient;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common.Authentication;
    using Sample.Common.Logging;
    using Sample.TestCallingBot.FrontEnd;
    using Sample.TestCallingBot.FrontEnd.Http;
    using CallerInfo = Sample.Common.Logging.CallerInfo;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    internal class Bot
    {
        /// <summary>
        /// The bot framework tenant for calling Skype consumer clients.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public const string BotFrameworkTenantId = "d6d49420-f39b-4df7-a1dc-d59a935871db";

        /// <summary>
        /// Gets or sets the OutboundPlayPromptContext.
        /// </summary>
        public const string OutboundPlayPromptContext = "OutboundCallWithPrompts";

        /// <summary>
        /// Prevents a default instance of the <see cref="Bot"/> class from being created.
        /// </summary>
        private Bot()
        {
            var logger = new GraphLogger(nameof(Bot));
            var builder = new StatefulClientBuilder("TestCallingBot", Service.Instance.Configuration.AadAppId, logger);
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

            this.AnswerWithMediaType = CallMediaType.Local;
            this.JoinedMediaType = CallMediaType.Local;

            var promptsBaseUri = $"https://{Service.Instance.Configuration.ServiceDnsName}/prompts";
            this.MediaMap["welcome"] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = $"{promptsBaseUri}/call_welcome.wav",
                    ResourceId = Guid.NewGuid().ToString(),
                },
                Loop = 1,
            };

            this.MediaMap["transferFailure"] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = $"{promptsBaseUri}/call_failure.wav",
                    ResourceId = Guid.NewGuid().ToString(),
                },
                Loop = 1,
            };

            this.MediaMap["OutboundPrompt"] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = $"{promptsBaseUri}/outbound_prompt.wav",
                    ResourceId = Guid.NewGuid().ToString(),
                },
                Loop = 1,
            };
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static Bot Instance { get; } = new Bot();

        /// <summary>
        /// Gets the prompts dictionary.
        /// </summary>
        public Dictionary<string, MediaPrompt> MediaMap { get; } = new Dictionary<string, MediaPrompt>();

        /// <summary>
        /// Gets the welcome prompts queue.
        /// </summary>
        public HashSet<string> WelcomePromptQueue { get; private set; } = new HashSet<string>();

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
        public IStatefulClient Client { get; }

        /// <summary>
        /// Gets or sets the type of the answer with media.
        /// </summary>
        /// <value>
        /// The type of the answer with media.
        /// </value>
        public CallMediaType AnswerWithMediaType { get; set; }

        /// <summary>
        /// Gets or sets the type of the joined media.
        /// </summary>
        /// <value>
        /// The type of the joined media.
        /// </value>
        public CallMediaType JoinedMediaType { get; set; }

        /// <summary>
        /// Inspect the exception type/error and return the correct response.
        /// </summary>
        /// <param name="exception">The caught exception.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage InspectExceptionAndReturnResponse(Exception exception)
        {
            HttpResponseMessage responseToReturn;
            if (exception is ServiceException e)
            {
                responseToReturn = (int)e.StatusCode >= 300
                    ? new HttpResponseMessage(e.StatusCode)
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);
                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        responseToReturn.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                responseToReturn.Content = new StringContent(e.ToString());
            }
            else
            {
                responseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(exception.ToString()),
                };
            }

            return responseToReturn;
        }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall"/> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallController.JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
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

            JoinMeetingParameters joinParams;

            if (joinCallBody.MediaType == CallMediaType.Local)
            {
                ILocalMediaSession mediaSession = this.CreateLocalMediaSession(correlationId);
                joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession);
            }
            else
            {
                var mediaToPrefetch = new List<MediaInfo>();
                foreach (var m in this.MediaMap)
                {
                    mediaToPrefetch.Add(m.Value.MediaInfo);
                }

                joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, new[] { Modality.Audio }, mediaToPrefetch);
            }

            this.JoinedMediaType = joinCallBody.MediaType;

            joinParams.RemoveFromDefaultAudioRoutingGroup = joinCallBody.RemoveFromDefaultRoutingGroup;
            joinParams.TenantId = joinCallBody.TenantId;
            joinParams.CorrelationId = correlationId;

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

            var statefulCall = await this.Client.Calls().AddAsync(joinParams).ConfigureAwait(false);
            if (joinCallBody.MediaType == CallMediaType.Remote)
            {
                this.WelcomePromptQueue.Add(statefulCall.Id);
            }

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Call creation complete: {statefulCall.Id}");
            return statefulCall;
        }

        /// <summary>
        /// Makes outgoing call asynchronously.
        /// </summary>
        /// <param name="makeCallBody">The outgoing call request body.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task MakeCallAsync(MakeCallController.MakeCallBody makeCallBody)
        {
            if (makeCallBody == null)
            {
                throw new ArgumentNullException(nameof(makeCallBody));
            }

            // A tracking id for logging purposes.  Helps identify this call in logs.
            var correlationId = Guid.NewGuid();

            var mediaToPrefetch = new List<MediaInfo>();
            foreach (var m in this.MediaMap)
            {
                mediaToPrefetch.Add(m.Value.MediaInfo);
            }

            var call = new Call
            {
                Targets = makeCallBody.Targets,
                MediaConfig = makeCallBody.MediaType == CallMediaType.Remote
                    ? new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch }
                    : null,
                RequestedModalities = new List<Modality> { Modality.Audio },
                CorrelationId = correlationId,
                TenantId = string.IsNullOrEmpty(makeCallBody.TenantId) ? BotFrameworkTenantId : makeCallBody.TenantId,
            };

            var appContext = OutboundPlayPromptContext;
            var statefulCall = default(ICall);

            if (makeCallBody.MediaType == CallMediaType.Remote)
            {
                statefulCall = await this.Client.Calls().AddAsync(call).ConfigureAwait(false);
            }
            else
            {
                ILocalMediaSession mediaSession = this.CreateLocalMediaSession(correlationId);
                statefulCall = await this.Client.Calls().AddAsync(call, mediaSession).ConfigureAwait(false);
            }

            this.AddCallHandler(statefulCall, appContext);
        }

        /// <summary>
        /// Transfers the call asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to transfer.</param>
        /// <param name="transferCallBody">The Transfers call body.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task TransferCallAsync(string callLegId, TransferController.TransferCallBody transferCallBody)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            // we have a blind transfer
            if (transferCallBody.FacilitateTransfer == true)
            {
                var appContext = "consultativeTransfer%" + callLegId;

                // store the current call id app context(echo'd back by the server) so that we know which call to replace when outgoing call completes
                var call = new Call
                {
                    RequestedModalities = new List<Modality> { Modality.Audio },
                    MediaConfig = new ServiceHostedMediaConfig(),
                    Targets = new List<InvitationParticipantInfo> { transferCallBody.Invitation },
                };
                var statefulCall = await this.Client.Calls().AddAsync(call).ConfigureAwait(false);

                this.AddCallHandler(statefulCall, appContext);
            }
            else
            {
                await this.Client.Calls()[callLegId].TransferAsync(transferCallBody.Invitation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds participants asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add participants.</param>
        /// <param name="invitation">The invitation.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        public async Task AddParticipantsAsync(string callLegId, InvitationParticipantInfo invitation)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId].Participants
                .InviteAsync(new[] { invitation })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Adds audio routing groups asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add audio routing group.</param>
        /// <param name="audioRoutingGroup">The audio routing group.</param>
        /// <returns>
        /// The routing group id.
        /// </returns>
        public async Task<string> AddAudioRoutingGroupAsync(string callLegId, AudioRoutingGroup audioRoutingGroup)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            var newRoutingGroup = await this.Client.Calls()[callLegId].AudioRoutingGroups.AddAsync(audioRoutingGroup).ConfigureAwait(false);
            return newRoutingGroup.Id;
        }

        /// <summary>
        /// updates audio routing groups asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add audio routing group.</param>
        /// <param name="audioRoutingGroup">The audio routing group.</param>
        /// <returns>
        /// The routing group id.
        /// </returns>
        public async Task UpdateAudioRoutingGroupAsync(string callLegId, AudioRoutingGroup audioRoutingGroup)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId].AudioRoutingGroups[audioRoutingGroup.RoutingMode.ToString()].UpdateAsync(audioRoutingGroup).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes audio routing groups asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add audio routing group.</param>
        /// <param name="routingMode">The audio group id to delete.</param>
        /// <returns>The routing group id.</returns>
        public async Task DeleteAudioRoutingGroupAsync(string callLegId, RoutingMode routingMode)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId].AudioRoutingGroups[routingMode.ToString()].DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Configures mixer asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to configure mixer.</param>
        /// <param name="configureMixerBody">The configure mixer request body.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ConfigureMixerAsync(string callLegId, ConfigureMixerController.ConfigureMixerBody configureMixerBody)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            if (string.IsNullOrEmpty(configureMixerBody.ReceivingParticipantId))
            {
                throw new ArgumentNullException(nameof(configureMixerBody.ReceivingParticipantId));
            }

            if (this.Client.Calls()[callLegId].Participants[configureMixerBody.ReceivingParticipantId] == null)
            {
                throw new InvalidOperationException("Participant doesn't exist in the collection.");
            }

            var mixerLevels = new List<ParticipantMixerLevel>();
            var participantMixerLevel = new ParticipantMixerLevel
            {
                Participant = configureMixerBody.ReceivingParticipantId,
                Ducking = configureMixerBody.Ducking,
                SourceLevels = new List<AudioSourceLevel> { new AudioSourceLevel { Participant = configureMixerBody.SourceParticipantId } },
            };
            mixerLevels.Add(participantMixerLevel);

            await this.Client.Calls()[callLegId].Participants
                .ConfigureMixerAsync(mixerLevels)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Mutes participants asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add participants.</param>
        /// <param name="muteBody">The mute participants body.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task MuteAsync(string callLegId, MuteController.MuteBody muteBody)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId].Participants
                .MuteAllAsync(muteBody.ParticipantIds)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Self unmute asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to unmute.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task UnmuteAsync(string callLegId)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            var selfId = this.Client.Calls()[callLegId].Resource.MyParticipantId;

            if (string.IsNullOrEmpty(selfId))
            {
                throw new ArgumentNullException(nameof(selfId));
            }

            await this.Client.Calls()[callLegId].Participants[selfId]
                .UnmuteAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribe to tone for a call asynchronously.
        /// </summary>
        /// <param name="callLegId">The call to subscribe tone for.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SubscribeToToneAsync(string callLegId)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            await this.Client.Calls()[callLegId].SubscribeToToneAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Get the image for a particular call.
        /// </summary>
        /// <param name="callLegId">
        /// The thread Id.
        /// </param>
        /// <returns>
        /// The screenshot data.
        /// </returns>
        internal Bitmap GetScreenshotByCallLegId(string callLegId)
        {
            return this.GetHandlerOrThrow(callLegId).LatestScreenshotImage;
        }

        /// <summary>
        /// Changes the answer media type for the bot.
        /// </summary>
        /// <param name="mediaType">
        /// The answer with media type, can be local or remote.
        /// </param>
        internal void SetAnswerWithMediaType(CallMediaType mediaType)
        {
            this.AnswerWithMediaType = mediaType;
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
        /// Change the video hue for a particular call.
        /// </summary>
        /// <param name="callLegId">
        /// The thread Id.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        internal void ChangeVideoHueByCallLegId(string callLegId, string color)
        {
            this.GetHandlerOrThrow(callLegId).SetHue(color);
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
            await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}"/> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            Task answerTask;

            var call = args.AddedResources.First();
            if (this.AnswerWithMediaType == CallMediaType.Local)
            {
                ILocalMediaSession mediaSession = this.CreateLocalMediaSession(call.CorrelationId);
                answerTask = call.AnswerAsync(mediaSession);
            }
            else
            {
                var mediaToPrefetch = new List<MediaInfo>();
                foreach (var m in this.MediaMap)
                {
                    mediaToPrefetch.Add(m.Value.MediaInfo);
                }

                answerTask = call.AnswerAsync(mediaToPrefetch, new[] { Modality.Audio });
                this.WelcomePromptQueue.Add(call.Id);
            }

            Task.Run(async () =>
            {
                try
                {
                    await answerTask.ConfigureAwait(false);
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, "Started answering incoming call");
                }
                catch (Exception ex)
                {
                    Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception happened when answering the call", ex);
                }
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
                this.AddCallHandler(call);
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
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>The <see cref="ILocalMediaSession"/>.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid correlationId)
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
                mediaSessionId: correlationId);
            return mediaSession;
        }

        /// <summary>
        /// Add call handler.
        /// </summary>
        /// <param name="call">Call instance.</param>
        /// <param name="appContext">Application context.</param>
        private void AddCallHandler(ICall call, string appContext = null)
        {
            var callHandler = this.CallHandlers.GetOrAdd(call.Id, new CallHandler(call));

            if (appContext != null)
            {
                callHandler.AppContext = appContext;
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
                throw new Exception($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}
