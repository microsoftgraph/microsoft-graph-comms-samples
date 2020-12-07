using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.GroupCallBot.Data;
using Microsoft.Graph.Communications.Core.Serialization;
using System.Collections.Generic;

namespace Sample.GroupCallBot
{
    public class Program
    {    

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
             WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddDebug();
                    logging.AddConsole();
                    logging.AddAzureWebAppDiagnostics();
                })
                .UseStartup<Startup>();
    }
}