// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="PlatformCallController.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************>

using Microsoft.Graph;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Notifications;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace RecordingBot.Services.Http.Controllers
{
    using ErrorConstants = Microsoft.Graph.Communications.Core.Exceptions.ErrorConstants;


    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    [RoutePrefix(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class PlatformCallController : ApiController
    {
        /// <summary>
        /// The bot service
        /// </summary>
        private readonly IBotService _botService;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;
        /// <summary>
        /// The event publisher
        /// </summary>
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.

        /// </summary>
        public PlatformCallController()
        {
            _botService = AppHost.AppHostInstance.Resolve<IBotService>();
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
            _eventPublisher = AppHost.AppHostInstance.Resolve<IEventPublisher>();
        }

        /// <summary>
        /// Handle a callback for an incoming call.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task<HttpResponseMessage> OnIncomingRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.RequestUri}";
            _logger.Info(log);
            _eventPublisher.Publish($"IncomingHTTP{this.Request.Method}", $"{this.Request.RequestUri}");

            // Instead of passing incoming notification to SDK, let's process it ourselves
            // so we can handle any policy evaluations.
            var response = await ProcessNotificationAsync(_botService.Client, this.Request).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Handle a callback for an existing call
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRequestRoute)]
        public async Task<HttpResponseMessage> OnNotificationRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.RequestUri}";
            _logger.Info(log);

            _eventPublisher.Publish($"NotificationHTTP{this.Request.Method}",  $"{this.Request.RequestUri}");

            //var content = await Request.Content.ReadAsStringAsync();
            //if (content.Contains("0#10550"))
            //{
            //    var callHandler = _botService.CallHandlers.First().Value;
            //    var bms = callHandler.BotMediaStream;
            //    bms.GetAudioQualityOfExperienceData();
            //}

            // Pass the incoming notification to the sdk. The sdk takes care of what to do with it.
            var response = await _botService.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Processes the notifications and raises the required callbacks.
        /// This function should be called in order for the SDK to raise
        /// any required events and process state changes.
        /// </summary>
        /// <param name="client">The stateful client.</param>
        /// <param name="request">The http request that is incoming from service.</param>
        /// <returns>Http Response Message after processed by the SDK. This has to
        /// be returned to the server.</returns>
        private static async Task<HttpResponseMessage> ProcessNotificationAsync(ICommunicationsClient client, HttpRequestMessage request)
        {
            client.NotNull(nameof(client));
            request.NotNull(nameof(request));
            var stopwatch = Stopwatch.StartNew();

            var scenarioId = client.GraphLogger.ParseScenarioId(request);
            var requestId = client.GraphLogger.ParseRequestId(request);

            CommsNotifications notifications = null;
            try
            {
                // Parse out the notification content.
                var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                var serializer = client.Serializer;
                notifications = NotificationProcessor.ExtractNotifications(content, serializer);
            }
            catch (ServiceException ex)
            {
                var statusCode = (int)ex.StatusCode >= 200
                    ? ex.StatusCode
                    : HttpStatusCode.BadRequest;
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, statusCode, stopwatch, ex);
            }
            catch (Exception ex)
            {
                var statusCode = HttpStatusCode.BadRequest;
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, statusCode, stopwatch, ex);
            }

            RequestValidationResult result;
            try
            {
                // Autenticate the incoming request.
                result = await client.AuthenticationProvider
                    .ValidateInboundRequestAsync(request)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var clientEx = new ClientException(
                    new Error
                    {
                        Code = ErrorConstants.Codes.ClientCallbackError,
                        Message = ErrorConstants.Messages.ClientErrorAuthenticatingRequest,
                    },
                    ex);

                throw clientEx;
            }

            if (!result.IsValid)
            {
                var statusCode = HttpStatusCode.Unauthorized;
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, statusCode, stopwatch);
            }

            // The request is valid. Let's evaluate any policies on the
            // incoming call before sending it off to the SDK for processing.
            var call = notifications?.Value?.FirstOrDefault()?.GetResourceData() as Call;
            var response = await EvaluateAndHandleIncomingCallPoliciesAsync(call).ConfigureAwait(false);
            if (response != null)
            {
                var level = client.GraphLogger.LogHttpRequest(request, response.StatusCode, notifications);
                client.GraphLogger.LogHttpResponse(level, request, response, stopwatch.ElapsedMilliseconds);
                stopwatch.Stop();
                return response;
            }

            try
            {
                var additionalData = request.GetHttpAndContentHeaders().ToDictionary(
                    pair => pair.Key,
                    pair => (object)string.Join(",", pair.Value),
                    StringComparer.OrdinalIgnoreCase);
                client.ProcessNotifications(request.RequestUri, notifications, result.TenantId, requestId, scenarioId, additionalData);
            }
            catch (ServiceException ex)
            {
                var statusCode = (int)ex.StatusCode >= 200
                    ? ex.StatusCode
                    : HttpStatusCode.InternalServerError;
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, statusCode, stopwatch, ex);
            }
            catch (Exception ex)
            {
                var statusCode = HttpStatusCode.InternalServerError;
                return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, statusCode, stopwatch, ex);
            }

            return client.LogAndCreateResponse(request, requestId, scenarioId, notifications, HttpStatusCode.Accepted, stopwatch);
        }

        /// <summary>
        /// Evaluate and perform any policies for the incoming call.
        /// </summary>
        /// <param name="call">The incoming call.</param>
        /// <returns>The <see cref="HttpResponseMessage" /> depending on the policy.</returns>
        private static async Task<HttpResponseMessage> EvaluateAndHandleIncomingCallPoliciesAsync(Call call)
        {
            if (string.IsNullOrWhiteSpace(call?.IncomingContext?.ObservedParticipantId))
            {
                // Call not associated with a participant (not a CR call).
                return null;
            }

            if (call.IncomingContext.ObservedParticipantId != call.IncomingContext?.SourceParticipantId || call.Source == null)
            {
                // Note: This should never happen.
                // Source participant is not the observed participant or
                // the source participant is missing.
                return null;
            }

            // Here we know the identity of the participant
            // we are observing and we can perform policy evaluations.
            var onBehalfOfIdentity = call.IncomingContext.OnBehalfOf;

            // The identity of the observed participant.
            var observedParticipantIdentity = call.Source.Identity;

            // The dynamic location of the participant.
            var observedParticipantLocation = call.Source.CountryCode;

            // Get the redirect location of the specified participant, if any.
            var redirectUri = await GetRedirectLocationOfParticipantAsync(
                onBehalfOfIdentity ?? observedParticipantIdentity,
                observedParticipantLocation).ConfigureAwait(false);

            if (redirectUri != null)
            {
                // This call should be redirected to another region.
                var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
                response.Headers.Location = redirectUri;
                return response;
            }

            return null;
        }

        /// <summary>
        /// Get the redirect location of the specified participant.
        /// </summary>
        /// <param name="identity">The identity of the participant.</param>
        /// <param name="countryCode">The dynamic location of the participant.</param>
        /// <returns>The redirect location for the specified participant.</returns>
        private static Task<Uri> GetRedirectLocationOfParticipantAsync(IdentitySet identity, string countryCode)
        {
            // TODO: add redirect logic.
            return Task.FromResult<Uri>(null);
        }
    }
}
