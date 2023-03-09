﻿// <copyright file="BotOptions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.SimpleIvrBot.Bot
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
        /// Gets or sets the calls uri of the application.
        /// </summary>
        public Uri BotBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the comms platform endpoint uri.
        /// </summary>
        public Uri PlaceCallEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets cosmosDB Configuration account uri.
        /// </summary>
        public string CosmosDBAccountUri { get; set; }

        /// <summary>
        /// Gets or sets cosmosDB Configuration account key.
        /// </summary>
        public string CosmosDBAccountKey { get; set; }

        /// <summary>
        /// Gets or sets cosmosDB Configuration database name.
        /// </summary>
        public string CosmosDBDatabaseName { get; set; }
    }
}
