using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    [ApiController]
    public class DemoController : Controller
    {
        private readonly IGraphLogger _logger;
        private readonly IBotService _botService;
        private readonly AzureSettings _settings;
        private readonly IEventPublisher _eventPublisher;

        public DemoController(IGraphLogger logger, IEventPublisher eventPublisher, IBotService botService, AzureSettings azureSettings)
        {
            _logger = logger;
            _eventPublisher = eventPublisher;
            _botService = botService;
            _settings = azureSettings;
        }

        [HttpGet]
        [Route(HttpRouteConstants.Calls + "/")]
        public IActionResult OnGetCalls()
        {
            _logger.Info("Getting calls");
            _eventPublisher.Publish("GetCalls", "Getting calls");

            var calls = new List<Dictionary<string, string>>();
            foreach (var callHandler in _botService.CallHandlers.Values)
            {
                var call = callHandler.Call;
                var callPath = "/" + HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id);
                var callUri = new Uri(_settings.CallControlBaseUrl, callPath).AbsoluteUri;
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "scenarioId", call.ScenarioId.ToString() },
                    { "call", callUri },
                    { "logs", callUri.Replace("/calls/", "/logs/") },
                };
                calls.Add(values);
            }

            return Ok(calls);
        }

        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<IActionResult> OnEndCallAsync(string callLegId)
        {
            var message = $"Ending call {callLegId}";
            _logger.Info(message);
            _eventPublisher.Publish("EndingCall", message);
            
            try
            {
                await _botService.EndCallByCallLegIdAsync(callLegId).ConfigureAwait(false);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.ToString());
            }
        }
    }
}
