// <copyright file="JoinCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Controllers
{
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Sample.HueBot.Bot;
    using Sample.HueBot.Extensions;

    /// <summary>
    /// JoinCallController is a third-party controller that can be called directly by the client or test app to trigger the bot to join a call.
    /// </summary>
    public class JoinCallController : Controller
    {
        private Bot bot;
        private StatefulServiceContext statefulServiceContext;
        private BotOptions botOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallController"/> class.
        /// </summary>
        /// <param name="bot">Bot instance.</param>
        /// <param name="statefulServiceContext">The service context.</param>
        /// <param name="botOptions">The bot options.</param>
        public JoinCallController(Bot bot, StatefulServiceContext statefulServiceContext, BotOptions botOptions)
        {
            this.bot = bot;
            this.statefulServiceContext = statefulServiceContext;
            this.botOptions = botOptions;
        }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>
        /// A <see cref="Task{TResult}" /> representing the result of the asynchronous operation.
        /// </returns>
        [HttpPost]
        [Route("joinCall")]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallBody joinCallBody)
        {
            var call = await this.bot.JoinCallAsync(joinCallBody).ConfigureAwait(false);
            return this.Ok(new JoinCallResponseBody
            {
                CallId = call.Id,
                CallHandlerPort = this.botOptions.BotBaseUrl.Port + this.statefulServiceContext.NodeInstance(),
            });
        }

        /// <summary>
        /// The join call body.
        /// Provide either:
        ///     1) JoinURL or
        ///     2) MeetingId and TenantId
        /// The second method is reserved for cloud-video-interop partners.
        /// The MeetingId is the short key generated for the room system devices.
        /// </summary>
        public class JoinCallBody
        {
            /// <summary>
            /// Gets or sets the join URL.
            /// </summary>
            public string JoinURL { get; set; }

            /// <summary>
            /// Gets or sets the meeting identifier.
            /// </summary>
            public string MeetingId { get; set; }

            /// <summary>
            /// Gets or sets the tenant id.
            /// </summary>
            public string TenantId { get; set; }

            /// <summary>
            /// Gets or sets the display name.
            /// Teams client does not allow changing of ones own display name.
            /// If display name is specified, we join as anonymous (guest) user
            /// with the specified display name.  This will put bot into lobby
            /// unless lobby bypass is disabled.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to remove the bot from default routing group.
            /// </summary>
            public bool RemoveFromDefaultRoutingGroup { get; set; }
        }

        /// <summary>
        /// The join call response body.
        /// </summary>
        public class JoinCallResponseBody
        {
            /// <summary>
            /// Gets or sets the call identifier for the newly created call.
            /// </summary>
            public string CallId { get; set; }

            /// <summary>
            /// Gets or sets the port to use to interact with this call (specific to this node).
            /// </summary>
            public int CallHandlerPort { get; set; }
        }
    }
}
