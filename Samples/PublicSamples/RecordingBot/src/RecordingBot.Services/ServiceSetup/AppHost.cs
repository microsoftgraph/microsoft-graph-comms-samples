using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Common.OData;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecordingBot.Services.ServiceSetup
{
    public class AppHost
    {
        private IServiceProvider ServiceProvider { get; set; }
        public static AppHost AppHostInstance { get; private set; }
        private IGraphLogger _logger;

        public AppHost()
        {
            AppHostInstance = this;
        }

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
                serverOptions.ListenAnyIP(azureSettings.CallSignalingPort + 1);
                serverOptions.ListenAnyIP(azureSettings.CallSignalingPort, config => config.UseHttps(azureSettings.Certificate));
            });

            // Add services to the container.
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true; //pretty
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.JsonSerializerOptions.Converters.Add(new ODataJsonConverterFactory(null, null, typeAssemblies: SerializerAssemblies.Assemblies));
            });

            builder.Services.AddCoreServices(builder.Configuration);

            var app = builder.Build();

            ServiceProvider = app.Services;

            _logger = Resolve<IGraphLogger>();

            try
            {
                Resolve<IEventPublisher>();
                Resolve<IBotService>().Initialize();
            }
            catch (Exception e)
            {
                app.Logger.LogError(e, "Unhandled exception in Boot()");
                return;
            }

            // Configure the HTTP request pipeline.
            app.UsePathBase(azureSettings.PodPathBase); 
            app.UsePathBase(azureSettings.ServicePath);
            
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseRouting();

            app.MapControllers();

            app.Run();
        }

        public T Resolve<T>()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}
