// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddParticipantsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   AddParticipantsController is a third-party controller (non-Bot Framework) that can be called to add participants to a meeting
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
    /// AddParticipantsController is a third-party controller (non-Bot Framework) that can be called to trigger a transfer.
    /// </summary>
    public class AddParticipantsController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The add participants async.
        /// </summary>
        /// <param name="callLegId">The call to add participants to.</param>
        /// <param name="invitation">The invitation.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnAddParticipantsRoute)]
        public async Task<HttpResponseMessage> AddParticipantsAsync(string callLegId, [FromBody] InvitationParticipantInfo invitation)
        {
            try
            {
                await Bot.Instance.AddParticipantsAsync(callLegId, invitation).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }
    }
}