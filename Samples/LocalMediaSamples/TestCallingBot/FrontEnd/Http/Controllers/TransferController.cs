// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransferController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   TransferController is a third-party controller (non-Bot Framework) that can be called to trigger a transfer
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
    /// TransferController is a third-party controller (non-Bot Framework) that can be called to trigger a transfer.
    /// </summary>
    public class TransferController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The transfer call async.
        /// </summary>
        /// <param name="callLegId">
        /// The call to transfer.
        /// </param>
        /// <param name="transferCallBody">
        /// The transfer call body.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnTransferCallRoute)]
        public async Task<HttpResponseMessage> TransferCallAsync(string callLegId, [FromBody] TransferCallBody transferCallBody)
        {
            try
            {
                await Bot.Instance.TransferCallAsync(callLegId, transferCallBody).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The Transfer call body.
        /// </summary>
        public class TransferCallBody
        {
            /// <summary>
            /// Gets or sets the invitation.
            /// </summary>
            public InvitationParticipantInfo Invitation { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [facilitate transfer].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [facilitate transfer]; otherwise, <c>false</c>.
            /// </value>
            public bool? FacilitateTransfer { get; set; }
        }
    }
}