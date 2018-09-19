// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MakeCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   MakeCallController is a third-party controller (non-Bot Framework) that makes an outbound call to a target
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph;
    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    ///   MakeCallController is a third-party controller (non-Bot Framework) that makes an outbound call to a target.
    /// </summary>
    public class MakeCallController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The making outbound call async.
        /// </summary>
        /// <param name="makeCallBody">
        /// The making outgoing call request body.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnMakeCallRoute)]
        public async Task<HttpResponseMessage> MakeOutgoingCallAsync([FromBody] MakeCallBody makeCallBody)
        {
            try
            {
                await Bot.Instance.MakeCallAsync(makeCallBody).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The outgoing call request body.
        /// </summary>
        public class MakeCallBody
        {
            /// <summary>
            /// Gets or sets the call targets.
            /// </summary>
            public IEnumerable<ParticipantInfo> Targets { get; set; }

            /// <summary>
            /// Gets or sets the call media type.
            /// </summary>
            public CallMediaType MediaType { get; set; }

            /// <summary>
            /// Gets or sets the call tenant. For calling consumer client, no tenant id is needed.
            /// </summary>
            public string TenantId { get; set; }
        }
    }
}