// <copyright file="IncidentWindowsService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// IncidentWindwosService class implementation.
    /// </summary>
    public class IncidentWindowsService
    {
        private static string baseUrl = "http://*:9442";
        private IWebHost webHost;
        private IConfigurationRoot configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentWindowsService"/> class.
        /// </summary>
        public IncidentWindowsService()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json");

            this.configuration = builder.Build();
            baseUrl = this.configuration["baseUrl"] != null ? this.configuration["baseUrl"] : baseUrl;
        }

        /// <summary>
        /// Build the web host.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The web host.</returns>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddDebug();
                    logging.AddConsole();
                    logging.AddAzureWebAppDiagnostics();
                })
                .UseStartup<Startup>()
                .UseUrls(baseUrl)
                .Build();

        /// <summary>
        /// Start method.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Start(string[] args)
        {
            try
            {
                this.webHost = BuildWebHost(args);
                this.webHost.Start();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stop method.
        /// </summary>
        public void Stop()
        {
            if (this.webHost != null)
            {
                this.webHost.Dispose();
            }
        }
    }
}
