// ***********************************************************************
// Assembly         : EchoBot.Bot
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 10-17-2023
// ***********************************************************************
// <copyright file="BotService.cs" company="Microsoft">
//     Copyright ©  2023
// </copyright>
// <summary></summary>
// ***********************************************************************
using EchoBot.Authentication;
using EchoBot.Constants;
using EchoBot.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using System.Collections.Concurrent;
using System.Net;
using EchoBot.Util;
using Microsoft.Graph.Models;
using Microsoft.Graph.Contracts;

namespace EchoBot.Bot
{
    /// <summary>
    /// Class BotService.
    /// Implements the <see cref="System.IDisposable" />
    /// Implements the <see cref="EchoBot.Bot.IBotService" />
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="EchoBot.Bot.IBotService" />
    public class BotService : IDisposable, IBotService
    {
        /// <summary>
        /// The Graph logger
        /// </summary>
        private readonly IGraphLogger _graphLogger;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly AppSettings _settings;

        /// <summary>
        /// Logger for logging media platform information
        /// </summary>
        private readonly IBotMediaLogger _mediaPlatformLogger;

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        public ICommunicationsClient Client { get; private set; }


        /// <summary>
        /// Dispose of the call client
        /// </summary>
        public void Dispose()
        {
            this.Client?.Dispose();
            this.Client = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotService" /> class.
        /// </summary>
        /// <param name="graphLogger"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <param name="mediaLogger"></param>
        public BotService(
            IGraphLogger graphLogger,
            ILogger<BotService> logger,
            IOptions<AppSettings> settings,
            IBotMediaLogger mediaLogger)
        {
            _graphLogger = graphLogger;
            _logger = logger;
            _settings = settings.Value;
            _mediaPlatformLogger = mediaLogger;
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing Bot Service");
            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                _settings.AadAppId,
                _graphLogger);

            var authProvider = new AuthenticationProvider(
                name,
                _settings.AadAppId,
                _settings.AadAppSecret,
                _graphLogger);

            var mediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = _settings.CertificateThumbprint,
                    InstanceInternalPort = _settings.MediaInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = _settings.MediaInstanceExternalPort,
                    ServiceFqdn = _settings.MediaDnsName
                },
                ApplicationId = _settings.AadAppId,
                MediaPlatformLogger = _mediaPlatformLogger
            };

            var notificationUrl = new Uri($"https://{_settings.ServiceDnsName}:{_settings.BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");
            _logger.LogInformation($"NotificationUrl: ${notificationUrl}");

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(notificationUrl);
            builder.SetMediaPlatformSettings(mediaPlatformSettings);
            builder.SetServiceBaseUrl(new Uri(AppConstants.PlaceCallEndpointUrl));

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;
        }

        /// <summary>
        /// Terminate all calls before and dispose of client
        /// </summary>
        /// <returns></returns>
        public async Task Shutdown()
        {
            _logger.LogWarning("Terminating all calls during shutdown event");
            await this.Client.TerminateAsync();
            this.Dispose();
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="threadId">The call thread id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        public async Task EndCallByThreadIdAsync(string threadId)
        {
            string callId = string.Empty;
            try
            {
                var callHandler = this.GetHandlerOrThrow(threadId);
                callId = callHandler.Call.Id;
                await callHandler.Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                if (!string.IsNullOrEmpty(callId))
                {
                    this.Client.Calls().TryForceRemove(callId, out ICall _);
                }
            }
        }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = Guid.NewGuid();

            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinUrl);

            var tenantId = (meetingInfo as OrganizerMeetingInfo).Organizer.GetPrimaryIdentity().GetTenantId();
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

            if (!this.CallHandlers.TryGetValue(joinParams.ChatInfo.ThreadId, out CallHandler? call))
            {
                var statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
                statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
                _logger.LogInformation($"Call creation complete: {statefulCall.Id}");
                return statefulCall;
            }

            throw new Exception("Call has already been added");
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">The media session identifier.
        /// This should be a unique value for each call.</param>
        /// <returns>The <see cref="ILocalMediaSession" />.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                // create media session object, this is needed to establish call connections
                return this.Client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = false //get the extra buffers for the speakers
                    },
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Inactive
                    },
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TResource}" /> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // Get the policy recording parameters.

                // The context associated with the incoming call.
                IncomingContext incomingContext =
                    call.Resource.IncomingContext;

                // The RP participant.
                string observedParticipantId =
                    incomingContext.ObservedParticipantId;

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity =
                    incomingContext.OnBehalfOf;

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity =
                    incomingContext.Transferor;

                string countryCode = null;
                EndpointType? endpointType = null;

                // Note: this should always be true for CR calls.
                if (incomingContext.ObservedParticipantId == incomingContext.SourceParticipantId)
                {
                    // The dynamic location of the RP.
                    countryCode = call.Resource.Source.CountryCode;

                    // The type of endpoint being used.
                    endpointType = call.Resource.Source.EndpointType;
                }

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection" /> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}" /> instance containing the event data.</param>
        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(call, _settings, _logger);
                var threadId = call.Resource.ChatInfo.ThreadId;
                this.CallHandlers[threadId] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                var threadId = call.Resource.ChatInfo.ThreadId;
                if (this.CallHandlers.TryRemove(threadId, out CallHandler? handler))
                {
                    Task.Run(async () => {
                        await handler.BotMediaStream.ShutdownAsync();
                        handler.Dispose();
                    });
                }
            }
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="threadId">The call thread id.</param>
        /// <returns>The <see cref="CallHandler" />.</returns>
        /// <exception cref="ArgumentException">call ({callLegId}) not found</exception>
        private CallHandler GetHandlerOrThrow(string threadId)
        {
            if (!this.CallHandlers.TryGetValue(threadId, out CallHandler? handler))
            {
                throw new ArgumentException($"call ({threadId}) not found");
            }

            return handler;
        }
    }
}

