// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubscribeToToneController.cs" company="Microsoft Corporation">
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

    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    public class SubscribeToToneController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The subcribe to tone call async.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnSubscribeToToneRoute)]
        public async Task<HttpResponseMessage> SubscribeToToneAsync(string callLegId)
        {
            try
            {
                await Bot.Instance.SubscribeToToneAsync(callLegId).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }
    }
}