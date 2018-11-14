// <copyright file="AddParticipantController.cs" company="Microsoft Corporation">
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
    /// AddParticipantController is a third-party controller (non-Bot Framework) that can be called to trigger a transfer.
    /// </summary>
    public class AddParticipantController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddParticipantController"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public AddParticipantController(Bot bot)
        {
            this.bot = bot;
        }

        /// <summary>
        /// The add participants async.
        /// </summary>
        /// <param name="callLegId">
        /// The call to add participants to.
        /// </param>
        /// <param name="addParticipantBody">
        /// The add participant request body.
        /// </param>
        /// <returns>The action result.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnAddParticipantRoute)]
        public async Task<IActionResult> AddParticipantAsync(string callLegId, [FromBody] AddParticipantRequestData addParticipantBody)
        {
            Validator.IsTrue(Guid.TryParse(callLegId, out Guid result), nameof(callLegId), "call leg id must be a valid guid.");
            Validator.NotNull(addParticipantBody, nameof(addParticipantBody));

            try
            {
                await this.bot.AddParticipantAsync(callLegId, addParticipantBody).ConfigureAwait(false);
                return this.Ok();
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }
    }
}