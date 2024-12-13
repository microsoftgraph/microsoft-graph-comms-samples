// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Service.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Service is the main entry point independent of Azure.  Anyone instantiating Service needs to first
//   initialize the DependencyResolver.  Calling Start() on the Service starts the HTTP server that will
//   listen for incoming Conversation requests from the Skype Platform.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd
{
    using System;
    using System.Net;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// Service is the main entry point independent of Azure.  Anyone instantiating Service needs to first
    /// initialize the DependencyResolver.  Calling Start() on the Service starts the HTTP server that will
    /// listen for incoming Conversation requests from the Skype Platform.
    /// </summary>
    public class Service
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly Service Instance = new Service();

        /// <summary>
        /// The sync lock.
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// The call http server.
        /// </summary>
        private IDisposable callHttpServer;

        /// <summary>
        /// Is the service started.
        /// </summary>
        private bool started;

        /// <summary>
        /// Graph logger instance.
        /// </summary>
        private IGraphLogger logger;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>
        /// Instantiate a custom server (e.g. for testing).
        /// </summary>
        /// <param name="config">The configuration to initialize.</param>
        /// <param name="logger">Logger instance.</param>
        public void Initialize(IConfiguration config, IGraphLogger logger)
        {
            this.Configuration = config;
            this.logger = logger;
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        public void Start()
        {
            lock (this.syncLock)
            {
                if (this.started)
                {
                    throw new InvalidOperationException("The service is already started.");
                }

                Bot.Bot.Instance.Initialize(this, this.logger);

                // Configure and start the HTTP server for calls using .NET 6.0 minimal hosting model
                var builder = WebApplication.CreateBuilder();

                // Configure Kestrel server options
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    foreach (var uri in this.Configuration.CallControlListeningUrls)
                    {
                        serverOptions.ListenAnyIP(uri.Port); // Listen on the configured port
                    }
                });

                // Add services to the DI container.
                builder.Services.AddControllers();
                var app = builder.Build();
                app.UseDeveloperExceptionPage();

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                // Start the web application
                app.Run();

                this.callHttpServer = app;
                this.started = true;
            }
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        public void Stop()
        {
            lock (this.syncLock)
            {
                if (!this.started)
                {
                    throw new InvalidOperationException("The service is already stopped.");
                }

                this.started = false;

                this.callHttpServer.Dispose();
                Bot.Bot.Instance.Dispose();
            }
        }
    }
}