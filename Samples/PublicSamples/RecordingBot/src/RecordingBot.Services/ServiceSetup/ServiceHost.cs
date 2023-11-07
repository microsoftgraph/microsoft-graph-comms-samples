// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="ServiceHost.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Services.Bot;
using RecordingBot.Services.Contract;
using RecordingBot.Services.Util;
using System;

namespace RecordingBot.Services.ServiceSetup
{
    /// <summary>
    /// Class ServiceHost.
    /// Implements the <see cref="RecordingBot.Services.Contract.IServiceHost" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Contract.IServiceHost" />
    public class ServiceHost : IServiceHost
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IServiceCollection Services { get; private set; }
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public IServiceProvider ServiceProvider { get; private set; }


        /// <summary>
        /// Configures the specified services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>ServiceHost.</returns>
        public ServiceHost Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("RecordingBot", redirectToTrace: true));
            services.Configure<AzureSettings>(configuration.GetSection(nameof(AzureSettings)));
            services.AddSingleton<IAzureSettings>(_ => _.GetRequiredService<IOptions<AzureSettings>>().Value);
			services.AddSingleton<IEventPublisher, EventGridPublisher>(_ => new EventGridPublisher(_.GetRequiredService<IOptions<AzureSettings>>().Value));
            services.AddSingleton<IBotService, BotService>();

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns>IServiceProvider.</returns>
        public IServiceProvider Build()
        {
            ServiceProvider = Services.BuildServiceProvider();
            return ServiceProvider;
        }
    }
}
