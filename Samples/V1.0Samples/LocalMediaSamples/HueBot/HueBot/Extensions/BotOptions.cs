// <copyright file="BotOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Bot
{
    using System;

    /// <summary>
    /// The bot options class.
    /// </summary>
    public class BotOptions
    {
        /// <summary>
        /// Gets or sets the application id.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the application secret.
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the callback URL of the application.
        /// E.g. https://your-bot-service.net/api/calls.
        /// </summary>
        public Uri BotBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the TCP address for media.
        /// E.g. net.tcp://your-bot-service.net:mediaPort.
        /// </summary>
        public Uri BotMediaProcessorUrl { get; set; }

        /// <summary>
        /// Gets or sets the public certificate for SSL.
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// Gets or sets the communications platform endpoint uri.
        /// E.g. https://graph.microsoft.com/v1.0.
        /// </summary>
        public Uri PlaceCallEndpointUrl { get; set; }
    }
}
