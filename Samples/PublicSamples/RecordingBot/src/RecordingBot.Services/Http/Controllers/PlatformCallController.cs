using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Extension;
using RecordingBot.Services.Contract;
using System;
using System.Threading.Tasks;

namespace RecordingBot.Services.Http.Controllers
{
    [ApiController]
    [Route(HttpRouteConstants.CALL_SIGNALING_ROUTE_PREFIX)]
    public class PlatformCallController : ControllerBase
    {
        private readonly IGraphLogger _logger;
        private readonly ICommunicationsClient _commsClient;

        public PlatformCallController(IGraphLogger logger, IBotService botService)
        {
            _logger = logger;
            _commsClient = botService.Client;
        }

        [HttpPost]
        [Route(HttpRouteConstants.ON_NOTIFICATION_REQUEST_ROUTE)]
        [Route(HttpRouteConstants.ON_INCOMING_REQUEST_ROUTE)]
        public async Task<IActionResult> OnNotificationRequestAsync(
           [FromHeader(Name = "Client-Request-Id")] Guid? clientRequestId,
           [FromHeader(Name = "X-Microsoft-Skype-Message-ID")] Guid? skypeRequestId,
           [FromHeader(Name = "Scenario-Id")] Guid? clientScenarioId,
           [FromHeader(Name = "X-Microsoft-Skype-Chain-ID")] Guid? skypeScenarioId,
           [FromBody] CommsNotifications notifications)
        {
            _logger.Info($"Received HTTP {Request.Method}, {Request.GetUrl()}");

            Guid requestId = clientRequestId ?? skypeRequestId ?? default;
            Guid scenarioId = clientScenarioId ?? skypeScenarioId ?? default;

            // Convert Request Authorization Request Header
            if (Request.Headers.Authorization.Count != 1)
            {
                return Unauthorized();
            }

            var schemeAndParameter = Request.Headers.Authorization[0].Split(" ");
            if (schemeAndParameter.Length != 2)
            {
                return Unauthorized();
            }

            RequestValidationResult result;

            var httpRequestMessage = new System.Net.Http.HttpRequestMessage();
            httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(schemeAndParameter[0],schemeAndParameter[1]);

            // Autenticate the incoming request.
            result = await _commsClient.AuthenticationProvider
                .ValidateInboundRequestAsync(httpRequestMessage)
                .ConfigureAwait(false);

            if (result.IsValid)
            {
                // Pass the incoming notification to the sdk. The sdk takes care of what to do with it.
                return Accepted(_commsClient.ProcessNotifications(Request.GetUri(), notifications, result.TenantId, requestId, scenarioId));
            }

            return Unauthorized();
        }
    }
}
