using Microsoft.Extensions.Configuration;
using Sample.AudioVideoPlaybackBot.FrontEnd;
using System;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;
using System.Threading;

namespace AVPWindowsService
{
    public partial class AudioVideoPlaybackService : ServiceBase
    {
        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The run complete event.
        /// </summary>
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        /// <summary>
        /// The nlog logger.
        /// </summary>
        //private static NLog.Logger nlogger = LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();

        public AudioVideoPlaybackService()
        {
            //this.logger = new GraphLogger(typeof(AudioVideoPlaybackService).Assembly.GetName().Name, redirectToTrace: true);
            //nlogger.Info("Nlogger initialized");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                base.RequestAdditionalTime(600000);
                Debugger.Launch();
                //nlogger.Warn("Nlogger inside OnStart");
                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddEnvironmentVariables();

                var configRoot = configurationBuilder.Build();

                var configs = new EnvironmentVarConfigs();
                configRoot.Bind("AzureSettings", configs);

                // ECS backend service enforced TLS 1.2 access.
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Create and start the environment-independent service.
                Service.Instance.Initialize(new WindowsServiceConfiguration(configs));
                Service.Instance.Start();

                base.OnStart(args);

            }
            catch (Exception e)
            {
                throw;
            }           
        }

        protected override void OnStop()
        {
            Service.Instance.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();
        }
    }
}
