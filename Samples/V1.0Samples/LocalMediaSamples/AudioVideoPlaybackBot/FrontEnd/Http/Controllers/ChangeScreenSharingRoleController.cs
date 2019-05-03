// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChangeScreenSharingRoleController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   ChangeScreenSharingRoleController is a third-party controller (non-Bot Framework) that changes the bot's screen sharing role
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Graph;

    using Sample.AudioVideoPlaybackBot.FrontEnd.Bot;

    /// <summary>
    /// ChangeScreenSharingRoleController is a third-party controller (non-Bot Framework) that changes the bot's screen sharing role.
    /// </summary>
    public class ChangeScreenSharingRoleController : ApiController
    {
        /// <summary>
        /// Changes screen sharing role.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg identifier.
        /// </param>
        /// <param name="changeRoleBody">
        /// The role to change to.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.CallRoute + "/" + HttpRouteConstants.OnChangeRoleRoute)]
        public async Task<HttpResponseMessage> ChangeScreenSharingRoleAsync(string callLegId, [FromBody] ChangeRoleBody changeRoleBody)
        {
            try
            {
                await Bot.Instance.ChangeSharingRoleAsync(callLegId, changeRoleBody.Role).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return e.InspectExceptionAndReturnResponse();
            }
        }

        /// <summary>
        /// Request body content to update screen sharing role.
        /// </summary>
        public class ChangeRoleBody
        {
            /// <summary>
            /// Gets or sets the role.
            /// </summary>
            /// <value>
            /// The role to change to.
            /// </value>
            public ScreenSharingRole Role { get; set; }
        }
    }
}