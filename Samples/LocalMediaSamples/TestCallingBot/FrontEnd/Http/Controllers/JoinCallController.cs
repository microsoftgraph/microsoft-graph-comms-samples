// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JoinCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call
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
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    public partial class JoinCallController : ApiController
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
        [Route(HttpRouteConstants.OnJoinCallRoute)]
        public async Task<HttpResponseMessage> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            try
            {
                var call = await Bot.Instance.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                return this.Request.CreateResponse(HttpStatusCode.OK, call.Id);
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// The join call body.
        /// </summary>
        public class JoinCallBody
        {
            /// <summary>
            /// Gets or sets the meeting identifier.
            /// </summary>
            public string MeetingId { get; set; }

            /// <summary>
            /// Gets or sets the tenant id.
            /// </summary>
            public string TenantId { get; set; }

            /// <summary>
            /// Gets or sets the chat info.
            /// </summary>
            public ChatInfo ChatInfo { get; set; }

            /// <summary>
            /// Gets or sets the organizer meeting info.
            /// </summary>
            public OrganizerMeetingInfo MeetingInfo { get; set; }

            /// <summary>
            /// Gets or sets the display name.
            /// Teams client does not allow changing of ones own display name.
            /// If display name is specified, we join as anonymous (guest) user
            /// with the specified display name.  This will put bot into lobby
            /// unless lobby bypass is disabled.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets the media stack used to join the call.
            /// </summary>
            public CallMediaType MediaType { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to remove the bot from default routing group.
            /// </summary>
            public bool RemoveFromDefaultRoutingGroup { get; set; }
        }
    }
}