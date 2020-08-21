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
    using Microsoft.Graph.Communications.Common;

    /// <summary>
    /// The incidents controller class.
    /// </summary>
    [Route("participantscalling")]
    public class ParticipantsCallingController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantsCallingController" /> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public ParticipantsCallingController(Bot bot, SampleObserver observer)
        {
            this.bot = bot;
        }

        /// <summary>
        /// Raise a request to call participants.
        /// </summary>
        /// <param name="participantsCallingRequestData">The incident data.</param>
        /// <returns>The action result.</returns>
        ///
        [HttpPost("raise")]
        public async Task<IActionResult> PostNotificationsAsync([FromBody] ParticipantsCallingRequestData participantsCallingRequestData)
        {
            try
            {
                Validator.NotNull(participantsCallingRequestData, nameof(participantsCallingRequestData), "participantsCallingRequestData is Null.");
                Validator.NotNull(participantsCallingRequestData.ObjectIds, nameof(participantsCallingRequestData.ObjectIds), "Object Ids are Null or Whitespace.");
                Validator.NotNullOrWhitespace(participantsCallingRequestData.TenantId, nameof(participantsCallingRequestData.TenantId), "Tenant Id is Null or Whitespace.");

                await this.bot.BotCallsUsersAsync(participantsCallingRequestData).ConfigureAwait(false);

                return this.Ok("Bot got a notification to call group of users.");
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }
    }
}
