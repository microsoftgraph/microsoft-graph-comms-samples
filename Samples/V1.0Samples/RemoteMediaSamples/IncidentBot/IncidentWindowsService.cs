// <copyright file="IncidentWindowsService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// IncidentWindwosService class implementation.
    /// </summary>
    public class IncidentWindowsService
    {
        private IWebHost webHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentWindowsService"/> class.
        /// </summary>
        public IncidentWindowsService()
        {
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
                .UseUrls("http://localhost:9442")
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
            this.webHost.Dispose();
        }
    }
}
