using EchoBot.Services.ServiceSetup;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace EchoBot
{
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static async Task Main(string[] args)
        {
            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            if (args.Length > 0 && args[0].Equals("-v"))
            {
                Console.WriteLine(info.FileVersion);
                return;
            }


            using (var channel = new InMemoryChannel())
            {
                try
                {
                    var builder = new ConfigurationBuilder();

                    // For local development app settings can be loaded
                    // with a .env file (which loads them into your environment variables)
                    // or with the app settings file

                    // this library will load the .env settings into the environment variables
                    //DotNetEnv.Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));

                    // you can use appsettings or environment variables
                    // tell the builder to look for the appsettings.json file
                    builder
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

                    var bot = new AppHost();
                    bot.Boot(builder, channel);
                    bot.StartServer();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("press any key to exit...");
                    Console.ReadKey();
                }
                finally
                {
                    channel.Flush();
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                }
            }
        }
    }
}
