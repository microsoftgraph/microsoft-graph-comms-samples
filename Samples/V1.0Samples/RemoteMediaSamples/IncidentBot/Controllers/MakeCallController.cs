// <copyright file="MakeCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Http
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Communications.Common;
    using Sample.IncidentBot.Bot;
    using Sample.IncidentBot.Data;

    /// <summary>
    ///   MakeCallController is a third-party controller (non-Bot Framework) that makes an outbound call to a target.
    /// </summary>
    public class MakeCallController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="MakeCallController"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public MakeCallController(Bot bot)
        {
            this.bot = bot;
        }

        // TODO: Reminder: make sure when calling this controller that the Content-Type of your request should be "application/json"

        /// <summary>
        /// The making outbound call async.
        /// </summary>
        /// <param name="makeCallBody">
        /// The making outgoing call request body.
        /// </param>
        /// <returns>
        /// The action result.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnMakeCallRoute)]
        public async Task<IActionResult> MakeOutgoingCallAsync([FromBody] MakeCallRequestData makeCallBody)
        {
            Validator.NotNull(makeCallBody, nameof(makeCallBody));

            try
            {
                await this.bot.MakeCallAsync(makeCallBody, Guid.NewGuid()).ConfigureAwait(false);
                return this.Ok();
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }
    }
}