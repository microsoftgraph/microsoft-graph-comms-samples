using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Exceptions;
using Microsoft.Kiota.Abstractions.Extensions;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Extension;
using RecordingBot.Model.Models;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    [ApiController]
    public class JoinCallController : ControllerBase
    {
        private readonly IGraphLogger _logger;
        private readonly IBotService _botService;
        private readonly AzureSettings _settings;
        private readonly IEventPublisher _eventPublisher;

        public JoinCallController(IGraphLogger logger, IEventPublisher eventPublisher, IBotService botService, AzureSettings azureSettings)
        {
            _logger = logger;
            _botService = botService;
            _settings = azureSettings;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        [Route(HttpRouteConstants.JOIN_CALLS)]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                var call = await _botService.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                var callPath = $"/{HttpRouteConstants.CALL_ROUTE.Replace("{callLegId}", call.Id)}";
                var callUri = $"{_settings.ServiceCname}{callPath}";

                _eventPublisher.Publish("JoinCall", $"Call.id = {call.Id}");

                return Ok(new JoinURLResponse
                {
                    Call = callUri,
                    CallId = call.Id,
                    ScenarioId = call.ScenarioId
                });
            }
            catch (ServiceException e)
            {
                var problemDetails = new ProblemDetails { Detail = e.ToString(), Status = (int)e.StatusCode };
                problemDetails.Extensions.AddOrReplace("responseHeaders", e.ResponseHeaders);

                return StatusCode(500, problemDetails);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Received HTTP {Request.Method}, {Request.GetUrl()}");

                return StatusCode(500, e.ToString());
            }
        }
    }
}
