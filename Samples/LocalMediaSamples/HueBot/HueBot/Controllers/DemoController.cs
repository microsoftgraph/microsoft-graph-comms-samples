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
    using Microsoft.Graph.Core.Telemetry;
    using Newtonsoft.Json;
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
        /// Logger instance.
        /// </summary>
        private readonly IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoController"/> class.
        /// </summary>
        /// <param name="bot">Bot instance.</param>
        /// <param name="logger">Logger instance.</param>
        public DemoController(Bot bot, IGraphLogger logger)
        {
            this.bot = bot;
            this.logger = logger;
        }

        /// <summary>
        /// Get the screenshot for the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to retrieve image.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.OnGetScreenshotRoute)]
        public ActionResult OnGetScreenshot(string callId)
        {
            this.logger.Info($"Retrieving image for call {callId}");

            try
            {
                var bitmap = this.bot.GetScreenshotByCallId(callId);
                if (bitmap == null)
                {
                    return this.NotFound();
                }

                this.Response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");
                this.Response.Headers.Add("Refresh", "3");

                var format = System.Drawing.Imaging.ImageFormat.Jpeg;

                var memStream = new MemoryStream();
                bitmap.Save(memStream, format);
                memStream.Position = 0;

                return this.File(memStream, $"image/{format}");
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
        [Route(HttpRouteConstants.OnPutHueRoute)]
        public IActionResult OnPutHue(string callId, [FromBody] string color)
        {
            this.logger.Info($"Changing hue for call {callId}");

            try
            {
                this.bot.ChangeVideoHueByCallId(callId, color);
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
            this.logger.Info($"Getting calls");

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
                    { "correlationId", call.CorrelationId.ToString() },
                    { "call", callUriTemplate },
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
        /// Change the hue of video for the call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to change hue.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpDelete]
        [Route(HttpRouteConstants.CallRoute)]
        public async Task<IActionResult> OnEndCallAsync(string callId)
        {
            this.logger.Info($"Ending call {callId}");

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
        /// Get the async outcomes log for a call.
        /// </summary>
        /// <param name="callId">
        /// Id of the call to retrieve image.
        /// </param>
        /// <param name="limit">
        /// Number of logs to retrieve (most recent).
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.CallRoute)]
        public IActionResult OnGetLog(string callId, int limit = 50)
        {
            this.logger.Info($"Retrieving image for thread id {callId}");

            try
            {
                this.Response.Headers.Add("Cache-Control", "private,must-revalidate,post-check=1,pre-check=2,no-cache");
                this.Response.Headers.Add("Refresh", $"1; url={this.Url}");

                return this.Content(
                    string.Join("\n\n==========================\n\n", this.bot.GetLogsByCallId(callId, limit)),
                    System.Net.Mime.MediaTypeNames.Text.Plain,
                    System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }
    }
}
