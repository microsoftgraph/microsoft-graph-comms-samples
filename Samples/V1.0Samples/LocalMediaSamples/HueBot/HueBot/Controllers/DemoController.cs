// <copyright file="DemoController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Newtonsoft.Json;
    using Sample.Common.Logging;
    using Sample.HueBot.Bot;

    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    public class DemoController : Controller
    {
        /// <summary>
        /// Bot instance.
        /// </summary>
        private readonly Bot bot;

        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly IGraphLogger logger;

        /// <summary>
        /// The observer instance.
        /// </summary>
        private readonly SampleObserver observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoController" /> class.
        /// </summary>
        /// <param name="bot">Bot instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="observer">The observer.</param>
        public DemoController(Bot bot, IGraphLogger logger, SampleObserver observer)
        {
            this.bot = bot;
            this.logger = logger;
            this.observer = observer;
        }

        /// <summary>
        /// Get the snapshot for the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to retrieve image.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.OnSnapshotRoute)]
        public ActionResult OnGetSnapshot(string callId)
        {
            this.logger.Info($"[{callId}] Retrieving snapshot image.");

            try
            {
                var bitmap = this.bot.GetScreenshotByCallId(callId);
                if (bitmap == null)
                {
                    return this.NotFound();
                }

                var format = System.Drawing.Imaging.ImageFormat.Jpeg;

                var memStream = new MemoryStream();
                bitmap.Save(memStream, format);
                memStream.Position = 0;

                this.AddRefreshHeader(3);
                return this.File(memStream, $"image/{format}");
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }

        /// <summary>
        /// Get the hue of video for the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to change hue.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.OnHueRoute)]
        public IActionResult OnGetHue(string callId)
        {
            this.logger.Info($"[{callId}] Get hue.");

            try
            {
                return this.Json(this.bot.GetVideoHueByCallId(callId));
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }

        /// <summary>
        /// Change the hue of video for the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to change hue.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpPut]
        [Route(HttpRouteConstants.OnHueRoute)]
        public IActionResult OnPutHue(string callId, [FromBody] string color)
        {
            this.logger.Info($"[{callId}] Set hue to {color}.");

            try
            {
                this.bot.SetVideoHueByCallId(callId, color);
                return this.Ok();
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }

        /// <summary>
        /// The on get calls.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Calls)]
        public IActionResult OnGetCalls()
        {
            this.logger.Info("Getting calls");

            if (this.bot.CallHandlers.IsEmpty)
            {
                return this.NoContent();
            }

            var calls = new List<Dictionary<string, string>>();
            foreach (var callHandler in this.bot.CallHandlers.Values)
            {
                var call = callHandler.Call;
                var callUriTemplate =
                    $"{this.Request.Scheme}://{this.Request.Host}/{HttpRouteConstants.CallRoute}"
                        .Replace("{callId}", call.Id);

                var values = new Dictionary<string, string>
                {
                    { "callId", call.Id },
                    { "scenarioId", call.ScenarioId.ToString() },
                    { "call", callUriTemplate },
                    { "logs", callUriTemplate.Replace("/calls/", "/logs/") },
                    { "screenshots", callUriTemplate + "scr" },
                    { "hue", callUriTemplate + "hue" },
                };
                calls.Add(values);
            }

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            return this.Json(calls, settings);
        }

        /// <summary>
        /// Allow the bot to hang up. This does not terminate the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to hang up.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<IActionResult> OnEndCallAsync(string callId)
        {
            this.logger.Info($"[{callId}] Ending call.");

            try
            {
                await this.bot.EndCallByCallIdAsync(callId).ConfigureAwait(false);
                return this.Ok();
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }

        /// <summary>
        /// Get logs from the service.
        /// </summary>
        /// <param name="skip">Skip specified lines.</param>
        /// <param name="take">Take specified lines.</param>
        /// <returns>Complete logs from the service.</returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs)]
        public IActionResult OnGetLogs(
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
        /// Get logs from the service.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="skip">Skip specified lines.</param>
        /// <param name="take">Take specified lines.</param>
        /// <returns>
        /// Complete logs from the service.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "{filter}")]
        public IActionResult OnGetLogs(
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
