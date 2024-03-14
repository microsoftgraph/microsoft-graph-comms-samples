using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using RecordingBot.Model.Models;
using RecordingBot.Services.Authentication;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RecordingBot.Services.Bot
{
    public class BotService : IDisposable, IBotService
    {
        private readonly IGraphLogger _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly AzureSettings _settings;

        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = [];
        public ICommunicationsClient Client { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Client?.Dispose();
            Client = null;
        }

        public BotService(IGraphLogger logger, IEventPublisher eventPublisher, IAzureSettings settings)
        {
            _logger = logger;
            _eventPublisher = eventPublisher;
            _settings = (AzureSettings)settings;
        }

        public void Initialize()
        {
            var name = GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(name, _settings.AadAppId, _logger);

            var authProvider = new AuthenticationProvider(name, _settings.AadAppId, _settings.AadAppSecret, _logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(_settings.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(_settings.MediaPlatformSettings);
            builder.SetServiceBaseUrl(_settings.PlaceCallEndpointUrl);

            Client = builder.Build();
            Client.Calls().OnIncoming += CallsOnIncoming;
            Client.Calls().OnUpdated += CallsOnUpdated;
        }

        public async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                Client.Calls().TryForceRemove(callLegId, out ICall _);
            }
        }

        public async Task<ICall> JoinCallAsync(JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = Guid.NewGuid();

            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);

            var tenantId = (meetingInfo as OrganizerMeetingInfo).Organizer.GetPrimaryIdentity().GetTenantId();
            var mediaSession = CreateLocalMediaSession();

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

            var statefulCall = await Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
            statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");

            return statefulCall;
        }

        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                // create media session object, this is needed to establish call connections
                return Client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Recvonly,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = true //get the extra buffers for the speakers
                    },
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Inactive
                    },
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception e)
            {
                _logger.Log(System.Diagnostics.TraceLevel.Error, e.Message);
                throw;
            }
        }

        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // Get the policy recording parameters.

                // The context associated with the incoming call.
                IncomingContext incomingContext = call.Resource.IncomingContext;

                // The RP participant.
                string observedParticipantId = incomingContext.ObservedParticipantId;

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity = incomingContext.OnBehalfOf;

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity = incomingContext.Transferor;

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

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId) ? CreateLocalMediaSession(callId) : CreateLocalMediaSession();

                // Answer call
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(call.GraphLogger, $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                CallHandlers[call.Id] = new CallHandler(call, _settings, _eventPublisher);
            }

            foreach (var call in args.RemovedResources)
            {
                if (CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }

        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ArgumentException($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}
