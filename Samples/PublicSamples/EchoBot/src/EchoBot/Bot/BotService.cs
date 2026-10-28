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
        /// Gets the collection of call handlers, keyed by call id. The call id always exists,
        /// unlike the chat thread id, which a call joined from a short meeting URL may not have yet.
        /// </summary>
        /// <value>The call handlers.</value>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Placeholder call id for a meeting whose join request is still in flight.
        /// </summary>
        private const string PendingCallId = "";

        /// <summary>
        /// Maps each joined meeting to the id of its call, so the same meeting is not joined twice.
        /// A long join URL is keyed by its chat thread; a short join URL, which carries no thread,
        /// is keyed by tenant and join meeting id, and its call also claims the thread key once the
        /// call exposes the thread.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _meetingCallIds = new ConcurrentDictionary<string, string>();

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
        /// <param name="threadId">The call thread id, or the call id returned when the call was joined.</param>
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

            // A short join URL resolves to JoinMeetingIdMeetingInfo, which carries no organizer, so
            // the tenant cannot be derived from the URL. Fall back to the tenant on the request body.
            var tenantId = (meetingInfo as OrganizerMeetingInfo)?.Organizer?.GetPrimaryIdentity()?.GetTenantId()
                ?? joinCallBody.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException(
                    "TenantId is required when joining with a short meeting URL, because the URL does not carry the organizer.",
                    nameof(joinCallBody));
            }

            var meetingKey = GetMeetingKey(chatInfo, meetingInfo, tenantId);

            // Reserve the meeting before creating the call, so a repeated or concurrent request for
            // the same meeting cannot create a second call. A short URL carries no thread, so a request
            // for a meeting already joined through its other URL form can only be detected once the
            // new call exposes its thread; ReserveMeetingThread then ends the duplicate call.
            if (!this.TryReserveMeeting(meetingKey))
            {
                throw new Exception("Call has already been added");
            }

            try
            {
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

                // The call may already have claimed this reservation when it was added to the collection.
                _meetingCallIds.TryUpdate(meetingKey, statefulCall.Id, PendingCallId);
                statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
                _logger.LogInformation($"Call creation complete: {statefulCall.Id}");
                return statefulCall;
            }
            catch
            {
                _meetingCallIds.TryRemove(new KeyValuePair<string, string>(meetingKey, PendingCallId));
                throw;
            }
        }

        /// <summary>
        /// Gets the key identifying the meeting being joined.
        /// </summary>
        /// <param name="chatInfo">The chat info, which is null for a short join URL.</param>
        /// <param name="meetingInfo">The meeting info.</param>
        /// <param name="tenantId">The tenant id of the meeting.</param>
        /// <returns>The meeting key.</returns>
        private static string GetMeetingKey(ChatInfo? chatInfo, MeetingInfo meetingInfo, string tenantId)
        {
            if (meetingInfo is JoinMeetingIdMeetingInfo joinMeetingIdMeetingInfo)
            {
                return $"joinMeetingId:{tenantId.ToLowerInvariant()}:{joinMeetingIdMeetingInfo.JoinMeetingId}";
            }

            return $"thread:{chatInfo?.ThreadId}";
        }

        /// <summary>
        /// Reserves a meeting for a new call, unless a call to it already exists or is being created.
        /// </summary>
        /// <param name="meetingKey">The meeting key.</param>
        /// <returns>True when the meeting was reserved.</returns>
        private bool TryReserveMeeting(string meetingKey)
        {
            while (!_meetingCallIds.TryAdd(meetingKey, PendingCallId))
            {
                if (!_meetingCallIds.TryGetValue(meetingKey, out var existingCallId))
                {
                    continue;
                }

                if (existingCallId == PendingCallId || this.Client.Calls()[existingCallId] != null)
                {
                    return false;
                }

                // The recorded call no longer exists, so the reservation is stale and can be replaced.
                if (_meetingCallIds.TryUpdate(meetingKey, PendingCallId, existingCallId))
                {
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Records the meeting's chat thread as the reservation for a call, once the call exposes it.
        /// The thread is the identity shared by the long and short forms of a meeting URL, but a call
        /// joined from the short URL only learns it after joining. If another live call already holds
        /// the thread, the same meeting was joined through the other URL form, so this call is ended.
        /// </summary>
        /// <param name="call">The call.</param>
        private void ReserveMeetingThread(ICall call)
        {
            var threadId = call.Resource?.ChatInfo?.ThreadId;
            if (string.IsNullOrEmpty(threadId))
            {
                return;
            }

            var threadKey = $"thread:{threadId}";
            while (true)
            {
                var existingCallId = _meetingCallIds.GetOrAdd(threadKey, call.Id);
                if (existingCallId == call.Id)
                {
                    return;
                }

                if (existingCallId != PendingCallId && this.Client.Calls()[existingCallId] != null)
                {
                    _logger.LogWarning($"Call {call.Id} is in meeting thread {threadId}, which call {existingCallId} has already joined. Ending the duplicate call.");
                    _ = call.DeleteAsync().ForgetAndLogExceptionAsync(call.GraphLogger, $"Ending duplicate call {call.Id}.");
                    return;
                }

                // Either the join that reserved this thread is still completing, possibly this call's
                // own join, or the recorded call has ended. Either way, the thread now belongs to this call.
                if (_meetingCallIds.TryUpdate(threadKey, call.Id, existingCallId))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Handles updates to a call, to reserve its meeting thread once the call exposes it.
        /// </summary>
        /// <param name="sender">The call.</param>
        /// <param name="args">The event args containing the call changes.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            this.ReserveMeetingThread(sender);
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
                this.CallHandlers[call.Id] = callHandler;
                call.OnUpdated += this.CallOnUpdated;
                this.ReserveMeetingThread(call);
            }

            foreach (var call in args.RemovedResources)
            {
                call.OnUpdated -= this.CallOnUpdated;

                foreach (var meetingCall in _meetingCallIds.Where(entry => entry.Value == call.Id).ToList())
                {
                    _meetingCallIds.TryRemove(meetingCall);
                }

                if (this.CallHandlers.TryRemove(call.Id, out CallHandler? handler))
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
        /// <param name="threadId">The call thread id, or the call id.</param>
        /// <returns>The <see cref="CallHandler" />.</returns>
        /// <exception cref="ArgumentException">call ({threadId}) not found</exception>
        private CallHandler GetHandlerOrThrow(string threadId)
        {
            if (this.CallHandlers.TryGetValue(threadId, out CallHandler? handler))
            {
                return handler;
            }

            return this.CallHandlers.Values.FirstOrDefault(callHandler => callHandler.Call.Resource.ChatInfo?.ThreadId == threadId)
                ?? throw new ArgumentException($"call ({threadId}) not found");
        }
    }
}

