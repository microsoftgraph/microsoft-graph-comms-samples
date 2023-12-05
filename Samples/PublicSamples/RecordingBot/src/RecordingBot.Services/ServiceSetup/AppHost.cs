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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Communications.Common.OData;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using System;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        /// Gets the application host instance.
        /// </summary>
        /// <value>The application host instance.</value>
        public static AppHost AppHostInstance { get; private set; }

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
        public void Boot(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            DotNetEnv.Env.Load(path: null, options: new DotNetEnv.LoadOptions());
            builder.Configuration.AddEnvironmentVariables();

            // Load Azure Settings
            var azureSettings = new AzureSettings();
            builder.Configuration.GetSection(nameof(AzureSettings)).Bind(azureSettings);
            azureSettings.Initialize();
            builder.Services.AddSingleton(azureSettings);

            // Setup Listening Urls
            builder.WebHost.UseKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(azureSettings.CallSignalingPort);
                // for local or debug operation we need an https port
                serverOptions.ListenAnyIP(azureSettings.CallSignalingPort - 1, config => config.UseHttps(azureSettings.Certificate));
            });

            // Add services to the container.
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true; //pretty
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.JsonSerializerOptions.Converters.Add(new ODataJsonConverterFactory(null, null, SerializerAssemblies.Assemblies));
            });

            var app = builder.Build();

            var host = new ServiceCollection().AddCoreServices(builder.Configuration);

            ServiceProvider = host.Build();

            _logger = Resolve<IGraphLogger>();

            try
            {
                Resolve<IEventPublisher>();
                _botService = Resolve<IBotService>();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unhandled exception in Boot()");
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void StartServer()
        {
            try
            {
                _botService.Initialize();
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
