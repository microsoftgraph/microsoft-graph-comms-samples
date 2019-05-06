// <copyright file="AzureAdAuthenticationBuilderExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Microsoft.AspNetCore.Authentication
{
    using System;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The azure ad service collection extensions class.
    /// </summary>
    public static class AzureAdAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Add azure ad bearer feature.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <returns>The updated authentication builder.</returns>
        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder)
            => builder.AddAzureAdBearer(_ => { });

        /// <summary>
        /// Add azure ad bearer feature.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="configureOptions">The configuration options.</param>
        /// <returns>The updated authentication builder.</returns>
        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();
            builder.AddJwtBearer();
            return builder;
        }

        /// <summary>
        /// The inner class for configuration Azure options.
        /// </summary>
        public class ConfigureAzureOptions : IConfigureNamedOptions<JwtBearerOptions>
        {
            private readonly AzureAdOptions azureOptions;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfigureAzureOptions"/> class.
            /// </summary>
            /// <param name="azureOptions">The Azure option.</param>
            public ConfigureAzureOptions(AzureAdOptions azureOptions)
            {
                this.azureOptions = azureOptions;
            }

            /// <summary>
            /// Configure the options.
            /// </summary>
            /// <param name="name">The name of options.</param>
            /// <param name="options">The options.</param>
            public void Configure(string name, JwtBearerOptions options)
            {
                options.Audience = this.azureOptions.AppId;
                options.Authority = $"{this.azureOptions.Instance}{this.azureOptions.TenantId}";
            }

            /// <summary>
            /// Configure the options.
            /// </summary>
            /// <param name="options">The options.</param>
            public void Configure(JwtBearerOptions options)
            {
                this.Configure(Options.DefaultName, options);
            }
        }
    }
}
