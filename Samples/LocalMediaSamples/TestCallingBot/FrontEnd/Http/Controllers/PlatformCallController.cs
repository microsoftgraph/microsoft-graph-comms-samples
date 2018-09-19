// <copyright file="PlatformCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph;
    using Microsoft.Graph.StatefulClient;
    using Sample.Common.Logging;
    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    /// Entry point for handling call-related web hook requests from the stateful client.
    /// </summary>
    public class PlatformCallController : ApiController
    {
        /// <summary>
        /// Gets a reference to singleton sample bot/client instance.
        /// </summary>
        private IStatefulClient Client => Bot.Instance.Client;

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.CallSignalingRoutePrefix + "/" + HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task<HttpResponseMessage> OnIncomingRequestAsync()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {this.Request.Method}, {this.Request.RequestUri}");

            // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
            var response = await this.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }
    }
}