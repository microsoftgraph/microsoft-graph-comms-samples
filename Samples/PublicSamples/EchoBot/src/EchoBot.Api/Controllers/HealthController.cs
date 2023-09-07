using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EchoBot.Api.Controllers
{

    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                _logger.LogInformation("HEALTH CALL");
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Received HTTP {this.Request.Method}, {this.Request.Path}");

                return Problem(detail: e.StackTrace, statusCode: (int)HttpStatusCode.InternalServerError, title: e.Message);
            }
        }
    }
}
