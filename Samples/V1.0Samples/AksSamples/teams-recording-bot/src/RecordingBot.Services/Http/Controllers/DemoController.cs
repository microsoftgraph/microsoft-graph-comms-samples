// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="DemoController.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>ScreenshotsController retrieves the screenshots stored by the bot</summary>
// ***********************************************************************-

using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Serialization;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    public class DemoController : ApiController
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
        /// Initializes a new instance of the <see cref="DemoController" /> class.

        /// </summary>
        public DemoController()
        {
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
            _eventPublisher = AppHost.AppHostInstance.Resolve<IEventPublisher>();
            _botService = AppHost.AppHostInstance.Resolve<IBotService>();
            _settings = AppHost.AppHostInstance.Resolve<IOptions<AzureSettings>>().Value;
        }

        /// <summary>
        /// The GET calls.
        /// </summary>
        /// <returns>The <see cref="Task" />.</returns>
        [HttpGet]
        [Route(HttpRouteConstants.Calls + "/")]
        public HttpResponseMessage OnGetCalls()
        {
            _logger.Info("Getting calls");
            _eventPublisher.Publish("GetCalls", "Getting calls");

            if (_botService.CallHandlers.IsEmpty)
            {
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

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

            var serializer = new CommsSerializer(pretty: true);
            var json = serializer.SerializeObject(calls);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        /// <summary>
        /// End the call.
        /// </summary>
        /// <param name="callLegId">Id of the call to end.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<HttpResponseMessage> OnEndCallAsync(string callLegId)
        {
            var message = $"Ending call {callLegId}";
            _logger.Info(message);
            _eventPublisher.Publish("EndingCall", message);
            
            try
            {
                await _botService.EndCallByCallLegIdAsync(callLegId).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                var response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(e.ToString());
                return response;
            }
        }
    }
}
