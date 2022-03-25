// ***********************************************************************
// Assembly         : EchoBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="PlatformCallController.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************>

using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using EchoBot.Model.Constants;
using EchoBot.Services.Contract;
using EchoBot.Services.ServiceSetup;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using EchoBot.Services.Extensions;

namespace EchoBot.Services.Http.Controllers
{
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
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.

        /// </summary>
        public PlatformCallController()
        {
            _botService = AppHost.AppHostInstance.Resolve<IBotService>();
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
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

            var response = await _botService.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            return await ControllerExtensions.GetActionResultAsync(this.Request, response).ConfigureAwait(false);
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

            // Pass the incoming notification to the sdk. The sdk takes care of what to do with it.
            var response = await _botService.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            return await ControllerExtensions.GetActionResultAsync(this.Request, response).ConfigureAwait(false);
        }
    }
}
