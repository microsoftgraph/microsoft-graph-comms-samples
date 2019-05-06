// <copyright file="PlatformCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph.Communications.Client;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Bot;

    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    [RoutePrefix(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class PlatformCallController : ApiController
    {
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        private IGraphLogger Logger => Bot.Instance.Logger;

        /// <summary>
        /// Gets a reference to singleton sample bot/client instance.
        /// </summary>
        private ICommunicationsClient Client => Bot.Instance.Client;

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]

        // [BotAuthentication]
        public async Task<HttpResponseMessage> OnIncomingRequestAsync()
        {
            this.Logger.Info($"Received HTTP {this.Request.Method}, {this.Request.RequestUri}");

            // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
            var response = await this.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }
    }
}