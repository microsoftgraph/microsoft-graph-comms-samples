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
    public class ServiceHost : IServiceHost
    {
        public IServiceCollection Services { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        public ServiceHost Configure(IServiceCollection services, IConfiguration configuration)
        {
            Services = services;
            Services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("RecordingBot", redirectToTrace: true));
            Services.AddSingleton<IAzureSettings>(_ => _.GetRequiredService<AzureSettings>());
            Services.AddSingleton<IEventPublisher, EventGridPublisher>(_ => new EventGridPublisher(_.GetRequiredService<IOptions<AzureSettings>>().Value));
            Services.AddSingleton<IBotService, BotService>();

            return this;
        }

        public IServiceProvider Build()
        {
            ServiceProvider = Services.BuildServiceProvider();
            return ServiceProvider;
        }
    }
}
