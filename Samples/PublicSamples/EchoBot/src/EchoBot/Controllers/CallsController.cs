// ***********************************************************************
// Assembly         : EchoBot.Controllers
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="JoinCallController.cs" company="Microsoft">
//     Copyright ©  2023
// </copyright>
// <summary></summary>
// ***********************************************************************
using EchoBot.Bot;
using EchoBot.Constants;
using EchoBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace EchoBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CallsController : ControllerBase
    {
        private readonly ILogger<CallsController> _logger;
        private readonly AppSettings _settings;
        private readonly IBotService _botService;

        public CallsController(ILogger<CallsController> logger,
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
                    ThreadId = call.Resource.ChatInfo.ThreadId,
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

        /// <summary>
        /// End the call.
        /// </summary>
        /// <param name="threadId">Thread Id of the call to end.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpDelete]
        public async Task<IActionResult> OnEndCallAsync(string threadId)
        {
            _logger.LogInformation($"Ending call {threadId}");

            try
            {
                await _botService.EndCallByThreadIdAsync(threadId).ConfigureAwait(false);
                return NoContent();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Received HTTP {this.Request.Method}, {this.Request.Path}");
                return Problem(detail: e.StackTrace, statusCode: (int)HttpStatusCode.InternalServerError, title: e.Message);
            }
        }
    }
}
