﻿// <copyright file="BotBuilderExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Sample.IncidentBot.Bot;

    /// <summary>
    /// The bot builder extensions class.
    /// </summary>
    public static class BotBuilderExtensions
    {
        /// <summary>
        /// Add bot feature.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddBot(this IServiceCollection services)
            => services.AddBot(_ => { });

        /// <summary>
        /// Add bot feature.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="botOptionsAction">The action for bot options.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddBot(this IServiceCollection services, Action<BotOptions> botOptionsAction)
        {
            var options = new BotOptions();
            botOptionsAction(options);
            services.AddSingleton(options);

            return services.AddSingleton<Bot>();
        }
    }
}
