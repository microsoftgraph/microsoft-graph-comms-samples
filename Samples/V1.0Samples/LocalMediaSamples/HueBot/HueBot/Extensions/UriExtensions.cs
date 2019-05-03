// <copyright file="UriExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Extensions
{
    using System;

    /// <summary>
    /// Extensions for URIs.
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Replaces the port in the URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// URI with replaced port.
        /// </returns>
        public static Uri ReplacePort(this Uri uri, int port)
        {
            var result = new UriBuilder(uri);
            result.Port = port;

            return result.Uri;
        }
    }
}
