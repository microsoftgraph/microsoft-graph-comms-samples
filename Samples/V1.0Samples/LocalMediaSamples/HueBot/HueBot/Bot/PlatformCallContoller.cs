// <copyright file="PlatformCallContoller.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Bot
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Communications.Client;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.HueBot.Controllers;
    using Sample.HueBot.Extensions;

    /// <summary>
    /// Entry point for handling call-related web hook requests from the stateful client.
    /// </summary>
    public class PlatformCallContoller : Controller
    {
        private IGraphLogger logger;
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallContoller"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="bot">Hue bot instance.</param>
        public PlatformCallContoller(IGraphLogger logger, Bot bot)
        {
            this.logger = logger;
            this.bot = bot;
        }

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.CallSignalingRoutePrefix + "/" + HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task OnIncomingRequestAsync()
        {
            this.logger.Info($"Received HTTP {this.Request.Method}, {this.Url}");

            // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
            var response = await this.bot.Client.ProcessNotificationAsync(this.Request.CreateRequestMessage()).ConfigureAwait(false);
            await response.CreateHttpResponseAsync(this.Response).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
        }
    }
}
