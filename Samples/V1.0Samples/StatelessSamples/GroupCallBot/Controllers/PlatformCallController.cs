// <copyright file="PlatformCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.GroupCallBot.Controller
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.GroupCallBot.Bot;

    /// <summary>
    /// Entry point for handling call-related web hook requests.
    /// </summary>
    public class PlatformCallController : Controller
    {
        private readonly IGraphLogger graphLogger;
        private readonly Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        public PlatformCallController(Bot bot)
        {
            this.bot = bot;
            this.graphLogger = bot.GraphLogger.CreateShim(nameof(PlatformCallController));
        }

        /// <summary>
        /// Handle call back for bot calls user case.
        /// </summary>
        /// <returns>returns when task is done.</returns>
        [HttpPost]
        [Route(ControllerConstants.CallbackPrefix)]
        public async Task OnIncomingBotCallUserRequestAsync()
        {
            await this.bot.ProcessNotificationAsync(this.Request, this.Response).ConfigureAwait(false);
        }
    }
}
