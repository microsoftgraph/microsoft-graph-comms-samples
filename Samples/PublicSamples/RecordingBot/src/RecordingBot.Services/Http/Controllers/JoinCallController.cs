// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="JoinCallController.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Extension;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Models;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph.Communications.Core.Exceptions;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    [ApiController]
    public class JoinCallController : ControllerBase
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;
        /// <summary>
        /// The bot service
        /// </summary>
        private readonly IBotService _botService;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly AzureSettings _settings;
        /// <summary>
        /// The event publisher
        /// </summary>
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="botService">The bot service.</param>
        /// <param name="settings">The settings.</param>
        public JoinCallController(IGraphLogger logger, IEventPublisher eventPublisher, IBotService botService, AzureSettings azureSettings)
        {
            _logger = logger;
            _botService = botService;
            _settings = azureSettings;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// The join call async.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.JoinCall)]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                var call = await _botService.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                var callPath = $"/{HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id)}";
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
                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        Response.Headers.Add(responseHeader.Key, new StringValues(responseHeader.Value.ToArray()));
                    }
                }

                return StatusCode((int)e.StatusCode < 300 ? 500 : (int)e.StatusCode, e.ToString());
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Received HTTP {this.Request.Method}, {this.Request.GetUri()}");
                return StatusCode(500, e.ToString());
            }
        }
    }
}
