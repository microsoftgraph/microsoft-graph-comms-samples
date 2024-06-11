// <copyright file="EnvironmentSettings.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Extensions
{
    /// <summary>
    /// Class EnvironmentSettings.
    /// </summary>
    public class EnvironmentSettings
    {
        /// <summary>
        /// Gets or sets the name of the service DNS.
        /// </summary>
        /// <value>The name of the service DNS.</value>
        public string ServiceDnsName { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>The certificate thumbprint.</value>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the aad application identifier.
        /// </summary>
        /// <value>The aad application identifier.</value>
        public string AadAppId { get; set; }

        /// <summary>
        /// Gets or sets the aad application secret.
        /// </summary>
        /// <value>The aad application secret.</value>
        public string AadAppSecret { get; set; }

        /// <summary>
        /// Gets or sets the Application Insights Instrumentation key
        /// used for logging to app insights.
        /// </summary>
        public string AppInsightsInstrumentationKey { get; set; }

        /// <summary>
        /// Gets or sets by the user only when using local dev settings
        /// since the media settings needs a different URI.
        /// </summary>
        public string MediaDnsName { get; set; }
    }
}
