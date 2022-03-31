using EchoBot.Services.ServiceSetup;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace EchoBot.WindowsService
{
    public partial class EchoBotService : ServiceBase
    {

        private AppHost _bot;

        public EchoBotService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
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
                    // For local development use the .env file
                    // Copy they .env-template and create a .env file
                    // this will load the .env settings into the environment variables
                    // DotNetEnv.Env.Load(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));

                    var builder = new ConfigurationBuilder();
                    builder.AddEnvironmentVariables();

                    _bot = new AppHost();
                    _bot.Boot(builder, channel);
                    _bot.StartServer();
                }
                finally
                {
                    channel.Flush();
                    Task.Run(async () => await Task.Delay(TimeSpan.FromMilliseconds(1000)));
                }
            }
        }

        protected override void OnStop()
        {
            _bot.StopServer();
        }
    }
}
