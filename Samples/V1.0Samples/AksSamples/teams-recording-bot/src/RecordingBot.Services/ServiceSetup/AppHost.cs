// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="AppHost.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Owin.Hosting;
using RecordingBot.Services.Contract;
using RecordingBot.Services.Http;
using System;

namespace RecordingBot.Services.ServiceSetup
{
    /// <summary>
    /// Class AppHost.
    /// </summary>
    public class AppHost
    {
        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        private IServiceProvider ServiceProvider { get; set; }
        /// <summary>
        /// Gets or sets the service collection.
        /// </summary>
        /// <value>The service collection.</value>
        private IServiceCollection ServiceCollection { get; set; }
        /// <summary>
        /// Gets the application host instance.
        /// </summary>
        /// <value>The application host instance.</value>
        public static AppHost AppHostInstance { get; private set; }

        /// <summary>
        /// The call HTTP server
        /// </summary>
        private IDisposable _callHttpServer;

        /// <summary>
        /// The settings
        /// </summary>
        private IAzureSettings _settings;
        /// <summary>
        /// The bot service
        /// </summary>
        private IBotService _botService;
        /// <summary>
        /// The logger
        /// </summary>
        private IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppHost" /> class.

        /// </summary>
        public AppHost()
        {
            AppHostInstance = this;
        }

        /// <summary>
        /// Boots this instance.
        /// </summary>
        public void Boot()
        {
            DotNetEnv.Env.Load(new DotNetEnv.Env.LoadOptions(parseVariables: false));

            var builder = new ConfigurationBuilder();

            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            ServiceCollection = new ServiceCollection();
            ServiceCollection.AddCoreServices(configuration);

            ServiceProvider = ServiceCollection.BuildServiceProvider();

            _logger = Resolve<IGraphLogger>();

            try
            {
                _settings = Resolve<IOptions<AzureSettings>>().Value;
                _settings.Initialize();
                Resolve<IEventPublisher>();
                _botService = Resolve<IBotService>();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unhandled exception in Boot()");
            }

        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            try
            {
                _botService.Initialize();

                var callStartOptions = new StartOptions();

                foreach (var url in ((AzureSettings)_settings).CallControlListeningUrls)
                {
                    callStartOptions.Urls.Add(url);
                    _logger.Info("Listening on: {url}", url);
                }

                _callHttpServer = WebApp.Start(
                    callStartOptions,
                    (appBuilder) =>
                    {
                        var startup = new HttpConfigurationInitializer();
                        startup.ConfigureSettings(appBuilder, _logger);
                    });
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unhandled exception in StartServer()");
                throw;
            }
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>T.</returns>
        public T Resolve<T>()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}
