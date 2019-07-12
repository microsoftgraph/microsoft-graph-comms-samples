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
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Core.Serialization;
    using Sample.Common.Logging;
    using Sample.IncidentBot;
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
        private SampleObserver observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentsController" /> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="observer">The log observer.</param>
        public IncidentsController(Bot bot, SampleObserver observer)
        {
            this.bot = bot;
            this.observer = observer;
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
                var call = await this.bot.RaiseIncidentAsync(incidentRequestData).ConfigureAwait(false);

                var callUriTemplate = new UriBuilder(this.bot.BotInstanceUri);
                callUriTemplate.Path = HttpRouteConstants.CallRoutePrefix.Replace("{callLegId}", call.Id);
                callUriTemplate.Query = this.bot.BotInstanceUri.Query.Trim('?');

                var callUri = callUriTemplate.Uri.AbsoluteUri;
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "scenarioId", call.ScenarioId.ToString() },
                    { "call", callUri },
                    { "logs", callUri.Replace("/calls/", "/logs/") },
                };

                var serializer = new CommsSerializer(pretty: true);
                var json = serializer.SerializeObject(values);
                return this.Ok(json);
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
        /// Get the service logs.
        /// </summary>
        /// <param name="skip">Skip specified lines.</param>
        /// <param name="take">Take specified lines.</param>
        /// <returns>The logs.</returns>
        [HttpGet]
        [Route("/logs")]
        public IActionResult GetLogs(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 1000)
        {
            this.AddRefreshHeader(3);
            return this.Content(
                this.observer.GetLogs(skip, take),
                System.Net.Mime.MediaTypeNames.Text.Plain,
                System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Get the service logs.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="skip">Skip specified lines.</param>
        /// <param name="take">Take specified lines.</param>
        /// <returns>
        /// The logs.
        /// </returns>
        [HttpGet]
        [Route("/logs/{filter}")]
        public IActionResult GetLogs(
            string filter,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 1000)
        {
            this.AddRefreshHeader(3);
            return this.Content(
                this.observer.GetLogs(filter, skip, take),
                System.Net.Mime.MediaTypeNames.Text.Plain,
                System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Add refresh headers for browsers to download content.
        /// </summary>
        /// <param name="seconds">Refresh rate.</param>
        private void AddRefreshHeader(int seconds)
        {
            this.Response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");
            this.Response.Headers.Add("Refresh", seconds.ToString());
        }
    }
}
