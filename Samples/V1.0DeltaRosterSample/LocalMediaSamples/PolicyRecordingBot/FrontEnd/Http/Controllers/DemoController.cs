// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   ScreenshotsController retrieves the screenshots stored by the bot
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Core.Serialization;
    using Sample.Common.Logging;
    using Sample.PolicyRecordingBot.FrontEnd.Bot;

    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    public class DemoController : ApiController
    {
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        private IGraphLogger Logger => Bot.Instance.Logger;

        /// <summary>
        /// Gets the sample log observer.
        /// </summary>
        private SampleObserver Observer => Bot.Instance.Observer;

        /// <summary>
        /// The GET logs.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <param name="take">The take.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/")]
        public HttpResponseMessage OnGetLogs(
            int skip = 0,
            int take = 1000)
        {
            var logs = this.Observer.GetLogs(skip, take);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(logs, Encoding.UTF8, "text/plain");
            return response;
        }

        /// <summary>
        /// The GET logs.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="take">The take.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/{filter}")]
        public HttpResponseMessage OnGetLogs(
            string filter,
            int skip = 0,
            int take = 1000)
        {
            var logs = this.Observer.GetLogs(filter, skip, take);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(logs, Encoding.UTF8, "text/plain");
            return response;
        }

        /// <summary>
        /// The GET calls.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Calls + "/")]
        public HttpResponseMessage OnGetCalls()
        {
            this.Logger.Info("Getting calls");

            if (Bot.Instance.CallHandlers.IsEmpty)
            {
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

            var calls = new List<Dictionary<string, string>>();
            foreach (var callHandler in Bot.Instance.CallHandlers.Values)
            {
                var call = callHandler.Call;
                var callPath = "/" + HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id);
                var callUri = new Uri(Service.Instance.Configuration.CallControlBaseUrl, callPath).AbsoluteUri;
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
        /// <param name="callLegId">
        /// Id of the call to end.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<HttpResponseMessage> OnEndCallAsync(string callLegId)
        {
            this.Logger.Info($"Ending call {callLegId}");

            try
            {
                await Bot.Instance.EndCallByCallLegIdAsync(callLegId).ConfigureAwait(false);

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