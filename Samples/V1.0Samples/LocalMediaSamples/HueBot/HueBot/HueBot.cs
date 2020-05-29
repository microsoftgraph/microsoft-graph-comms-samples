// <copyright file="HueBot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Sample.Common.Logging;
    using Sample.HueBot.Bot;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class HueBot : StatelessService
    {
        private IGraphLogger logger;
        private SampleObserver observer;
        private IConfiguration configuration;
        private BotOptions botOptions;
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="HueBot" /> class.
        /// </summary>
        /// <param name="context">Stateless service context from service fabric.</param>
        /// <param name="logger">Global logger instance.</param>
        /// <param name="observer">Global observer instance.</param>
        public HueBot(StatelessServiceContext context, IGraphLogger logger, SampleObserver observer)
            : base(context)
        {
            this.logger = logger;
            this.observer = observer;

            // Set directory to where the assemblies are running from.
            // This is necessary for Media binaries to pick up logging configuration.
            var location = Assembly.GetExecutingAssembly().Location;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(location));
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            this.configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            this.botOptions = this.configuration.GetSection("Bot").Get<BotOptions>();

            this.bot = new Bot(this.botOptions, this.logger, this.Context);

            var serviceInstanceListeners = new List<ServiceInstanceListener>();
            foreach (string endpointName in new[] { "ServiceEndpoint", "SignalingPort", "LocalEndpoint" })
            {
                serviceInstanceListeners.Add(new ServiceInstanceListener(
                    serviceContext =>
                        new HttpSysCommunicationListener(serviceContext, endpointName, (url, listener) =>
                        {
                            ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting web listener on {url}");
                            return this.CreateHueBotWebHost(url);
                        }),
                    endpointName));
            }

            return serviceInstanceListeners.ToArray();
        }

        /// <summary>
        /// Creates the hue bot web host.
        /// </summary>
        /// <param name="url">The URL to host at.</param>
        /// <returns>web host.</returns>
        private IWebHost CreateHueBotWebHost(string url)
        {
            // Reference https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication-aspnetcore
            return new WebHostBuilder()
                .UseHttpSys(options =>
                {
                    // Copied from https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys
                    options.Authentication.Schemes = AuthenticationSchemes.None;
                    options.Authentication.AllowAnonymous = true;
                    options.MaxConnections = 1000;
                    options.MaxRequestBodySize = 30000000;
                })
                .ConfigureServices(
                    services => services
                        .AddSingleton(this.logger)
                        .AddSingleton(this.observer)
                        .AddSingleton(this.Context)
                        .AddSingleton(this.botOptions)
                        .AddSingleton(this.bot)
                        .AddMvc())
                .Configure(app => app
                    .UseDeveloperExceptionPage() // Disable this on production environments.
                    .UseMvc())
                .UseConfiguration(this.configuration)
                .UseUrls(url)
                .Build();
        }
    }
}
