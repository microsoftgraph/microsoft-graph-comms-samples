// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureMixerController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   ConfigureMixerController is a third-party controller (non-Bot Framework) that configure mixers for participants in meetings
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph;
    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    /// ConfigureMixerController is a third-party controller (non-Bot Framework) that configure mixers for participants in meetings.
    /// </summary>
    public class ConfigureMixerController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The configure mixer async.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <param name="configureMixerBody">
        /// The configure mixer request body.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnConfigureMixerRoute)]
        public async Task<HttpResponseMessage> ConfigureMixerAsync(string callLegId, [FromBody] ConfigureMixerBody configureMixerBody)
        {
            try
            {
                await Bot.Instance.ConfigureMixerAsync(callLegId, configureMixerBody).ConfigureAwait(false);
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
        public class ConfigureMixerBody
        {
            /// <summary>
            /// Gets or sets the receiver participant id.
            /// </summary>
            public string ReceivingParticipantId { get; set; }

            /// <summary>
            /// Gets or sets the source participant id.
            /// </summary>
            public string SourceParticipantId { get; set; }

            /// <summary>
            /// Gets or sets the audio ducking configuration.
            /// </summary>
            public AudioDuckingConfiguration Ducking { get; set; }
        }
    }
}