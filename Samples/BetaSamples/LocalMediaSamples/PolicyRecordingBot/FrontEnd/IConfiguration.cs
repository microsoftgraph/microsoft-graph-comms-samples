// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   IConfiguration contains the static configuration information the application needs
//   to run such as the urls it needs to listen on, credentials to communicate with
//   Bing translator, settings for media.platform, etc.
//   The concrete implemenation AzureConfiguration gets the configuration from Azure.  However,
//   other concrete classes could be created to allow the application to run outside of Azure
//   for testing.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Skype.Bots.Media;

    /// <summary>
    /// IConfiguration contains the static configuration information the application needs
    /// to run such as the URLs it needs to listen on, credentials to communicate with
    /// Bing translator, settings for media.platform, etc.
    /// <para></para>
    /// The concrete implementation AzureConfiguration gets the configuration from Azure.  However,
    /// other concrete classes could be created to allow the application to run outside of Azure
    /// for testing.
    /// </summary>
    public interface IConfiguration : IDisposable
    {
        /// <summary>
        /// Gets the DNS name for this service.
        /// </summary>
        string ServiceDnsName { get; }

        /// <summary>
        /// Gets the List of HTTP URLs the app should listen on for incoming call
        /// signaling requests from Skype Platform.
        /// </summary>
        IEnumerable<Uri> CallControlListeningUrls { get; }

        /// <summary>
        /// Gets the base callback URL for this instance.  To ensure that all requests
        /// for a given call go to the same instance, this Url is unique to each
        /// instance by way of its instance input endpoint port.
        /// </summary>
        Uri CallControlBaseUrl { get; }

        /// <summary>
        /// Gets the remote endpoint that any outgoing call targets.
        /// </summary>
        Uri PlaceCallEndpointUrl { get; }

        /// <summary>
        /// Gets the AadAppId generated at the time of registration of the bot.
        /// </summary>
        string AadAppId { get; }

        /// <summary>
        /// Gets the AadAppSecret generated at the time of registration of the bot.
        /// </summary>
        string AadAppSecret { get; }

        /// <summary>
        /// Gets the Settings for the bot media platform.
        /// </summary>
        MediaPlatformSettings MediaPlatformSettings { get; }
    }
}