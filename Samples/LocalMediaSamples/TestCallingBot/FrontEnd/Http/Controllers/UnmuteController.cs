// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnmuteController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   UnmuteParticipantController is a third-party controller (non-Bot Framework) that can be called to self unmute in a meeting
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    /// UnmuteParticipantController is a third-party controller (non-Bot Framework) that can be called to self unmute in a meeting.
    /// </summary>
    public class UnmuteController : ApiController
    {
        /// <summary>
        /// The mute participants async.
        /// </summary>
        /// <param name="callLegId">
        /// The call to unmute participant on.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnUnmute)]
        public async Task<HttpResponseMessage> UnmuteAsync(string callLegId)
        {
            try
            {
                await Bot.Instance.UnmuteAsync(callLegId).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }
    }
}