// <copyright file="ConfigurationManager.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.SimpleIvrBot.Data
{
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The incident request data.
    /// </summary>
    public static class ConfigurationManager
    {
        /// <summary>
        /// Gets the AppSetting data.
        /// </summary>
        public static IConfiguration AppSetting { get; }

#pragma warning disable SA1201 // Elements should appear in the correct order
                              /// <summary>
                              /// Initializes static members of the <see cref="ConfigurationManager"/> class.
                              /// </summary>
        static ConfigurationManager()
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            AppSetting = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json")
                  .Build();
        }
    }
}