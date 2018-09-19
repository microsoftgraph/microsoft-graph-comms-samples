// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AudioGroupsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   AudioGroupsController is a third-party controller (non-Bot Framework) that can be called to add or update audio groups for a call
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
    /// AudioGroupsController is a third-party controller (non-Bot Framework) that can be called to add or update audio groups for a call.
    /// </summary>
    public class AudioGroupsController : ApiController
    {
        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The create audio group async.
        /// </summary>
        /// <param name="callLegId">The call to add audio routes to.</param>
        /// <param name="audioRoutingGroup">The audio routing group.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnAddAudioRoutingGroupRoute)]
        public async Task<HttpResponseMessage> AddAudioRoutingGroupAsync(string callLegId, [FromBody] AudioRoutingGroup audioRoutingGroup)
        {
            try
            {
                var returnedAudioGroupId = await Bot.Instance.AddAudioRoutingGroupAsync(callLegId, audioRoutingGroup).ConfigureAwait(false);
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(returnedAudioGroupId);
                return response;
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The update audio group async.
        /// </summary>
        /// <param name="callLegId">The call to update audio routes on.</param>
        /// <param name="audioRoutingGroup">The audio routing group.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        [HttpPut]
        [Route(HttpRouteConstants.OnUpdateAudioRoutingGroupRoute)]
        public async Task<HttpResponseMessage> UpdateAudioRoutingGroupAsync(string callLegId, [FromBody] AudioRoutingGroup audioRoutingGroup)
        {
            try
            {
                await Bot.Instance.UpdateAudioRoutingGroupAsync(callLegId, audioRoutingGroup).ConfigureAwait(false);
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The delete audio group async.
        /// </summary>
        /// <param name="callLegId">
        /// The call to delete audio routes.
        /// </param>
        /// <param name="routingMode">
        /// The delete audio groups id.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpDelete]
        [Route(HttpRouteConstants.OnDeleteAudioRoutingGroupRoute)]
        public async Task<HttpResponseMessage> DeleteAudioRoutingGroupAsync(string callLegId, [FromBody] RoutingMode routingMode)
        {
            try
            {
                await Bot.Instance.DeleteAudioRoutingGroupAsync(callLegId, routingMode).ConfigureAwait(false);
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }
    }
}