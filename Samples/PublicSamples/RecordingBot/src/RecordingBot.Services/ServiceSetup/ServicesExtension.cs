using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecordingBot.Services.Contract;
using System;

namespace RecordingBot.Services.ServiceSetup
{
    public static class ServicesExtension
    {
        public static ServiceHost AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            return new ServiceHost().Configure(services, configuration);
        }

        public static TConfig ConfigureConfigObject<TConfig>(this IServiceCollection services, IConfiguration configuration)
            where TConfig : class, new()
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            var config = new TConfig();
            configuration.Bind(config);

            if (config is IInitializable init)
            {
                init.Initialize();
            }

            services.AddSingleton(config);
            return config;
        }

        public static TConfig ConfigureConfigObject<TConfig>(this IServiceCollection services)
            where TConfig : class, new()
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            var config = new TConfig();

            if (config is IInitializable init)
            {
                init.Initialize();
            }

            services.AddSingleton(config);
            return config;
        }
    }
}
