// <copyright file="UsersController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.ReminderBot.Controller
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Sample.ReminderBot.Bot;
    using Sample.ReminderBot.Data;
    using Sample.ReminderBot.Extensions;
    using Microsoft.Graph.Communications.Common;

    /// <summary>
    /// The incidents controller class.
    /// </summary>
    [Route("User")]
    public class UsersController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController" /> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public UsersController(Bot bot)
        {
            this.bot = bot;
        }

        /// <summary>
        /// Raise a flow for reminding user.
        /// </summary>
        /// <param name="userRequestData">The user data.</param>
        /// <returns>The action result.</returns>
        [HttpPost("raise")]
        public async Task<IActionResult> PostNotificationsAsync([FromBody] UserRequestData userRequestData)
        {
            try
            {
                Validator.NotNull(userRequestData, nameof(userRequestData),"UserRequestData is Null.");
                Validator.NotNullOrWhitespace(userRequestData.ObjectId, nameof(userRequestData.ObjectId),"Object Id is Null or Whitespace.");
                Validator.NotNullOrWhitespace(userRequestData.TenantId, nameof(userRequestData.TenantId),"Tenant Id is Null or Whitespace.");

                await this.bot.BotCallsUsersAsync(userRequestData).ConfigureAwait(false);

                return this.Ok("Bot got a notification to remind the user.");
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }


    }
}
