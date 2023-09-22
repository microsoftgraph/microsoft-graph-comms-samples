using EchoBot.Bot;
using EchoBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace EchoBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JoinCallController : ControllerBase
    {
        private readonly ILogger<JoinCallController> _logger;
        private readonly AppSettings _settings;
        private readonly IBotService _botService;

        public JoinCallController(ILogger<JoinCallController> logger,
            IOptions<AppSettings> settings,
            IBotService botService)
        {
            _logger = logger;
            _settings = settings.Value;
            _botService = botService;
        }

        /// <summary>
        /// The join call async.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        //[Route(HttpRouteConstants.JoinCall)]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                _logger.LogInformation("JOIN CALL");
                var call = await _botService.JoinCallAsync(joinCallBody).ConfigureAwait(false);

                var values = new
                {
                    CallId = call.Id,
                    ScenarioId = call.ScenarioId,
                    Port = _settings.BotInstanceExternalPort.ToString()
                };

                return Ok(values);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Received HTTP {this.Request.Method}, {this.Request.Path}");

                return Problem(detail: e.StackTrace, statusCode: (int)HttpStatusCode.InternalServerError, title: e.Message);
            }
        }
    }
}
