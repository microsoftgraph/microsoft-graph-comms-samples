// <copyright file="HomeController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.VoiceRecorderAndPlaybackBot.Controller
{
    using Microsoft.AspNetCore.Mvc;
    using Sample.Common.Logging;

    /// <summary>
    /// The home controller class.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly SampleObserver observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="observer">The observer.</param>
        public HomeController(SampleObserver observer)
        {
            this.observer = observer;
        }

        /// <summary>
        /// Get the default content of home page.
        /// </summary>
        /// <returns>Default content.</returns>
        [HttpGet("/")]
        public string Get()
        {
            return "Home Page";
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
