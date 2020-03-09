// <copyright file="ParticipantsCallingController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.GroupCallBot.Controller
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Sample.Common.Logging;
    using Sample.GroupCallBot.Bot;
    using Sample.GroupCallBot.Data;
    using Sample.GroupCallBot.Extensions;

    /// <summary>
    /// The incidents controller class.
    /// </summary>
    [Route("participantscalling")]
    public class ParticipantsCallingController : Controller
    {
        private Bot bot;
        private SampleObserver observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantsCallingController" /> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="observer">The log observer.</param>
        public ParticipantsCallingController(Bot bot, SampleObserver observer)
        {
            this.bot = bot;
            this.observer = observer;
        }

        /// <summary>
        /// Raise a incident.
        /// </summary>
        /// <param name="participantsCallingRequestData">The incident data.</param>
        /// <returns>The action result.</returns>
        ///
        [HttpPost("raise")]
        public async Task<IActionResult> PostNotificationsAsync([FromBody] ParticipantsCallingRequestData participantsCallingRequestData)
        {
            try
            {
                await this.bot.BotCallsUsersAsync(participantsCallingRequestData).ConfigureAwait(false);

                return this.Ok("Bot got notifications");
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }
    }
}
