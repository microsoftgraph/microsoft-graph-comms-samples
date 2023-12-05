// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="PlatformCallController.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************>

using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Extension;
using System;

namespace RecordingBot.Services.Http.Controllers
{
    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    [ApiController]
    [Route(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class PlatformCallController : ControllerBase
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        private readonly ICommunicationsClient _commsClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.
        /// </summary>
        public PlatformCallController(IGraphLogger logger, ICommunicationsClient commsClient)
        {
            _logger = logger;
            _commsClient = commsClient;
        }

        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRequestRoute)]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]
        public IActionResult OnNotificationRequestAsync(
           [FromHeader(Name = "Client-Request-Id")] Guid? clientRequestId,
           [FromHeader(Name = "X-Microsoft-Skype-Message-ID")] Guid? skypeRequestId,
           [FromHeader(Name = "Scenario-Id")] Guid? clientScenarioId,
           [FromHeader(Name = "X-Microsoft-Skype-Chain-ID")] Guid? skypeScenarioId,
           [FromBody] CommsNotifications notifications)
        {
            _logger.Info($"Received HTTP {Request.Method}, {Request.GetUri()}");

            Guid requestId = clientRequestId ?? skypeRequestId ?? default;
            Guid scenarioId = clientScenarioId ?? skypeScenarioId ?? default;

            // Pass the incoming notification to the sdk. The sdk takes care of what to do with it.
            return Accepted(_commsClient.ProcessNotifications(Request.GetUri(), notifications, User.Identity!.Name!, requestId, scenarioId));
        }
    }
}
