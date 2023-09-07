using EchoBot.Api.Bot;
using EchoBot.Api.ServiceSetup;
using EchoBot.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Common.Telemetry;
using System.Diagnostics;
using System.Runtime;
using System.Security.Cryptography.X509Certificates;

namespace EchoBot.Api;
public class BotHost
{
    private WebApplication? _app;

    public BotHost()
    {

    }

    public void Start()
    {
        //DotNetEnv.Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));
        //var pathArray = Environment.CurrentDirectory.Split('/');
        //pathArray[pathArray.Length - 1] += ".Api";
        //var basee = string.Join('/', pathArray);
        //var basePath = AppDomain.CurrentDomain.BaseDirectory.Replace(Environment.CurrentDirectory, basee);
        //string basePath = Environment.CurrentDirectory;
        //var index = basePath.LastIndexOf("EchoBot");
        //if (index > 0)
        //    basePath = basePath.Substring(index, )
        //var c = System.Environment.CurrentDirectory;
        //var d = System.Environment.SystemDirectory;
        //var b = AppDomain.CurrentDomain.BaseDirectory.Split('/');
        //var ewew = b.(x => x == AppDomain.CurrentDomain.FriendlyName);
        //var index1 = AppDomain.CurrentDomain.BaseDirectory.LastIndexOf(AppDomain.CurrentDomain.FriendlyName);
        //var path = AppDomain.CurrentDomain.BaseDirectory.Substring(index1)
        //var a = builder.Environment.ContentRootPath.TrimEnd('/') + ".Api/";
        //var basePath = string.Join("/", pathArray);
        //builder.Configuration.SetBasePath(basee);// "/Users/bcage/git/bcage29/EchoBotVNext/EchoBot/EchoBot.Api/bin/Debug/net6.0");
        //var webAppOptions = new WebApplicationOptions()
        //{
        //    ContentRootPath = basee
        //};

        
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddEnvironmentVariables(prefix: "AppSettings__");//prefix: "CustomPrefix_"

        // Add services to the container.
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(JoinCallController).Assembly);
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //var appSettings = builder.Configuration.GetSection(nameof(AppSettings)) as AppSettings;

        var appSettings = builder.Configuration.Get<AppSettings>();

        builder.Services
            .AddOptions<AppSettings>()            
            .BindConfiguration(nameof(AppSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger(typeof(BotHost).Assembly.GetName().Name, redirectToTrace: true));
        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddSingleton<IBotMediaLogger, BotMediaLogger>();

        //builder.Logging.AddApplicationInsights();
        //builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        builder.Services.AddSingleton<IBotService, BotService>();

        var botSettings = new BotSettings(appSettings);

        //var urls = new[]
        //{
        //    "http://localhost:5000",
        //    "http://localhost:8181"
        //};
        
        builder.WebHost.UseUrls(botSettings.CallControlListeningUrls);

        var cert = GetCertificateFromStore(appSettings.CertificateThumbprint);
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(listenOptions =>
            {
                listenOptions.ServerCertificate = cert;
            });
        });


        _app = builder.Build();

       
        // initialize the bot
        //var mediaLogger = _app.Services.GetRequiredService<IBotMediaLogger>();
        var bot = _app.Services.GetRequiredService<IBotService>();
        bot.Initialize();
        
        //_app.Services.GetRequiredService<I>; ;

        // Configure the HTTP request pipeline.
        //if (_app.Environment.IsDevelopment())
        //{
            _app.UseSwagger();
            _app.UseSwaggerUI();
        //}

        

        //_app.UseHttpsRedirection();

        //_app.UseRouting();

        _app.UseAuthorization();

        _app.MapControllers();

        _app.Run();
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
        }
    }

    /// <summary>
    /// Helper to search the certificate store by its thumbprint.
    /// </summary>
    /// <returns>Certificate if found.</returns>
    /// <exception cref="Exception">No certificate with thumbprint {CertificateThumbprint} was found in the machine store.</exception>
    private X509Certificate2 GetCertificateFromStore(string certificateThumbprint)
    {

        X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        try
        {
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, validOnly: false);

            if (certs.Count != 1)
            {
                throw new Exception($"No certificate with thumbprint {certificateThumbprint} was found in the machine store.");
            }

            return certs[0];
        }
        finally
        {
            store.Close();
        }
    }
}
