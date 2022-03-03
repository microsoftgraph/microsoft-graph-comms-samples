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

using Microsoft.Graph.Communications.Common.Telemetry;
using EchoBot.Model.Constants;
using EchoBot.Services.ServiceSetup;
using System.Net.Http;
using System.Web.Http;
using System.Net;

namespace EchoBot.Services.Http.Controllers
{
    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    public class HealthController : ApiController
    {
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.

        /// </summary>
        public HealthController()
        {
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
        }

        /// <summary>
        /// Handle a callback for an incoming call.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpGet]
        [Route(HttpRouteConstants.HealthRoute)]
        public HttpResponseMessage Health()
        {
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
