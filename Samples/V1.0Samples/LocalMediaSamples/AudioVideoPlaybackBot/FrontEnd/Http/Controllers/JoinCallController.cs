// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JoinCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Core.Serialization;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Bot;

    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    public class JoinCallController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The join call async.
        /// </summary>
        /// <param name="joinCallBody">
        /// The join call body.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.JoinCall)]
        public async Task<HttpResponseMessage> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Serving {HttpRouteConstants.JoinCall}", EventLogEntryType.Information);
            try
            {
                var call = await Bot.Instance.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                var callPath = "/" + HttpRouteConstants.CallRoute.Replace("{callLegId}", call.Id);
                var callUri = new Uri(Service.Instance.Configuration.CallControlBaseUrl, callPath).AbsoluteUri;
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "scenarioId", call.ScenarioId.ToString() },
                    { "call", callUri },
                    { "logs", callUri.Replace("/calls/", "/logs/") },
                    { "changeScreenSharingRole", callUri + "/" + HttpRouteConstants.OnChangeRoleRoute },
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
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(e.ToString());
                return response;
            }
        }

        /// <summary>
        /// The join call body.
        /// Provide either:
        ///     1) JoinURL or
        ///     2) VideoTeleconferenceId and TenantId
        /// The second method is reserved for cloud-video-interop partners.
        /// The VideoTeleconferenceId is the short key generated for the room system devices.
        /// </summary>
        public class JoinCallBody
        {
            /// <summary>
            /// Gets or sets the VTC Id.
            /// This id is used to retrieve meeting info through Graph endpoint.
            /// Please see README regarding how to obtain a VTC Id.
            /// </summary>
            public string VideoTeleconferenceId { get; set; }

            /// <summary>
            /// Gets or sets the tenant id.
            /// The tenant id is needed to acquire authentication to get meeting info.
            /// </summary>
            public string TenantId { get; set; }

            /// <summary>
            /// Gets or sets the Teams meeting join URL.
            /// This URL is used to join a Teams meeting.
            /// </summary>
            public string JoinURL { get; set; }

            /// <summary>
            /// Gets or sets the display name.
            /// Teams client does not allow changing of ones own display name.
            /// If display name is specified, we join as anonymous (guest) user
            /// with the specified display name.  This will put bot into lobby
            /// unless lobby bypass is disabled.
            /// </summary>
            public string DisplayName { get; set; }
        }
    }
}