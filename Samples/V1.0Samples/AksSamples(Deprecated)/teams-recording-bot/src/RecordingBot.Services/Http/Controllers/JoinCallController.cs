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
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Serialization;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Models;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    public class JoinCallController : ApiController
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
        public JoinCallController()
        {
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
            _eventPublisher = AppHost.AppHostInstance.Resolve<IEventPublisher>();
            _botService = AppHost.AppHostInstance.Resolve<IBotService>();
            _settings = AppHost.AppHostInstance.Resolve<IOptions<AzureSettings>>().Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallController" /> class.

        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="botService">The bot service.</param>
        /// <param name="settings">The settings.</param>
        public JoinCallController(IGraphLogger logger, IEventPublisher eventPublisher, IBotService botService, IAzureSettings settings)
        {
            _logger = logger;
            _botService = botService;
            _settings = (AzureSettings)settings;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// The join call async.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.JoinCall)]
        public async Task<HttpResponseMessage> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                var call = await _botService.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                var callPath = $"/{HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id)}";
                var callUri = $"{_settings.ServiceCname}{callPath}";
                _eventPublisher.Publish("JoinCall", $"Call.id = {call.Id}");

                var values = new JoinURLResponse()
                {
                    Call = callUri,
                    CallId = call.Id,
                    ScenarioId = call.ScenarioId
                };

                var serializer = new CommsSerializer(pretty: true);
                var json = serializer.SerializeObject(values);
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return response;
            }
            catch (ServiceException e)
            {
                HttpResponseMessage response = (int)e.StatusCode >= 300
                    ? this.Request.CreateResponse(e.StatusCode)
                    : this.Request.CreateResponse(HttpStatusCode.InternalServerError);

                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        response.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                response.Content = new StringContent(e.ToString());
                return response;
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Received HTTP {this.Request.Method}, {this.Request.RequestUri}");
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }
    }
}
