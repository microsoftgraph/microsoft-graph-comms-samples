// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MuteController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   MuteParticipantsController is a third-party controller (non-Bot Framework) that can be called to mute participants in a meeting
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

    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    ///   MuteParticipantsController is a third-party controller (non-Bot Framework) that can be called to mute participants in a meeting.
    /// </summary>
    public class MuteController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The mute participants async.
        /// </summary>
        /// <param name="callLegId">
        /// The call to mute participants on.
        /// </param>
        /// <param name="muteBody">
        /// The mute participants request body.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnMute)]
        public async Task<HttpResponseMessage> MuteAsync(string callLegId, [FromBody] MuteBody muteBody)
        {
            try
            {
                await Bot.Instance.MuteAsync(callLegId, muteBody).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The add participant request body.
        /// </summary>
        public class MuteBody
        {
            /// <summary>
            /// Gets or sets the participant ids.
            /// </summary>
            public IEnumerable<string> ParticipantIds { get; set; }
        }
    }
}