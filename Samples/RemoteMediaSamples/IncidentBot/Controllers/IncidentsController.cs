// <copyright file="IncidentsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace IcMBot.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Core;
    using Sample.IncidentBot.Bot;
    using Sample.IncidentBot.Data;
    using Sample.IncidentBot.IncidentStatus;

    /// <summary>
    /// The incidents controller class.
    /// </summary>
    [Route("[controller]")]
    public class IncidentsController : Controller
    {
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentsController"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public IncidentsController(Bot bot)
        {
            this.bot = bot;
        }

        /// <summary>
        /// Raise a incident.
        /// </summary>
        /// <param name="incidentRequestData">The incident data.</param>
        /// <returns>The action result.</returns>
        [HttpPost("raise")]
        public async Task<IActionResult> PostIncidentAsync([FromBody] IncidentRequestData incidentRequestData)
        {
            Validator.NotNull(incidentRequestData, nameof(incidentRequestData));

            try
            {
                var botMeetingCall = await this.bot.RaiseIncidentAsync(incidentRequestData).ConfigureAwait(false);

                return this.Ok(botMeetingCall.Id);
            }
            catch (Exception e)
            {
                return this.Exception(e);
            }
        }

        /// <summary>
        /// Gets a collection of incidents.
        /// </summary>
        /// <param name="maxCount">The maximum count of insidents in return values.</param>
        /// <returns>The incident status collection.</returns>
        [HttpGet]
        public async Task<IEnumerable<IncidentStatusData>> GetRecentIncidentsAsync(int maxCount = 100)
        {
            return await Task.FromResult(this.bot.IncidentStatusManager.GetRecentIncidents(maxCount)).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the responder status.
        /// </summary>
        /// <param name="callId">The call id.</param>
        /// <param name="maxCount">The maximum count of log lines.</param>
        /// <returns>The logs.</returns>
        [HttpGet]
        [Route("/log/calls/{callId}")]
        public async Task<IEnumerable<string>> GetCallDetailsAsync(string callId, int maxCount = 1000)
        {
            Validator.IsTrue(Guid.TryParse(callId, out Guid result), nameof(callId), "call id must be a valid guid.");

            return await Task.FromResult(this.bot.GetLogsByCallLegId(callId, maxCount)).ConfigureAwait(false);
        }
    }
}
