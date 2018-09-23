// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   ScreenshotsController retrieves the screenshots stored by the bot
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph.CoreSDK.Serialization;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Bot;
    using Sample.Common.Logging;

    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    public class DemoController : ApiController
    {
        /// <summary>
        /// The on get calls.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Calls + "/")]
        public HttpResponseMessage OnGetCalls()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Getting calls");

            if (Bot.Instance.CallHandlers.IsEmpty)
            {
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

            var calls = new List<Dictionary<string, string>>();
            foreach (var callHandler in Bot.Instance.CallHandlers.Values)
            {
                var call = callHandler.Call;
                var callUriTemplate =
                    (Service.Instance.Configuration.CallControlBaseUrl + "/" +
                    HttpRouteConstants.CallRoute)
                        .Replace(HttpRouteConstants.CallSignalingRoutePrefix + "/", string.Empty)
                        .Replace("{callLegId}", call.Id);
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "correlationId", call.CorrelationId.ToString() },
                    { "call", callUriTemplate },
                    { "changeScreenSharingRole", callUriTemplate + "/" + HttpRouteConstants.OnChangeRoleRoute },
                };
                calls.Add(values);
            }

            var serializer = new Serializer(pretty: true);
            var json = serializer.SerializeObject(calls);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        /// <summary>
        /// Change the hue of video for the call.
        /// </summary>
        /// <param name="callLegId">
        /// Id of the call to change hue.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<HttpResponseMessage> OnEndCallAsync(string callLegId)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Ending call {callLegId}");

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

        /// <summary>
        /// Get the async outcomes log for a call.
        /// </summary>
        /// <param name="callLegId">
        /// Id of the call to retrieve image.
        /// </param>
        /// <param name="limit">
        /// Number of logs to retrieve (most recent).
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.CallRoute)]
        public HttpResponseMessage OnGetLog(string callLegId, int limit = 50)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Retrieving image for thread id {callLegId}");

            try
            {
                var response = this.Request.CreateResponse(HttpStatusCode.OK);

                response.Content = new StringContent(string.Join("\n\n==========================\n\n", Bot.Instance.GetLogsByCallLegId(callLegId, limit)), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Text.Plain);

                response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");

                response.Headers.Add("Refresh", $"1; url={this.Request.RequestUri}");

                return response;
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