// <copyright file="JoinCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Http
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Core;
    using Sample.IncidentBot.Bot;
    using Sample.IncidentBot.Data;

    /// <summary>
    /// JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call.
    /// </summary>
    public class JoinCallController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallController"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public JoinCallController(Bot bot)
        {
            this.bot = bot;
        }

        /// <summary>
        /// The join call async.
        /// </summary>
        /// <param name="joinCallBody">
        /// The join call body.
        /// </param>
        /// <returns>
        /// The action result.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnJoinCallRoute)]
        public async Task<IActionResult> JoinCallAsync([FromBody] JoinCallRequestData joinCallBody)
        {
            Validator.NotNull(joinCallBody, nameof(joinCallBody));

            try
            {
                var call = await this.bot.JoinCallAsync(joinCallBody).ConfigureAwait(false);
                return this.Ok(call.Id);
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }
    }
}