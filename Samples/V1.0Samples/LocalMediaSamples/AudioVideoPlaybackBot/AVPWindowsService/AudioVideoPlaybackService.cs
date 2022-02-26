using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Communications.Common.Telemetry;
using Sample.AudioVideoPlaybackBot.FrontEnd;
using Sample.AudioVideoPlaybackBot.WorkerRole;
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
        /// The graph logger.
        /// </summary>
        private readonly IGraphLogger logger;

        public AudioVideoPlaybackService()
        {
            this.logger = new GraphLogger(typeof(AudioVideoPlaybackService).Assembly.GetName().Name, redirectToTrace: true);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
#if DEBUG
                base.RequestAdditionalTime(600000);
                Debugger.Launch();
#endif

                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;
                IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddEnvironmentVariables();

                var configRoot = configurationBuilder.Build();

                var azConfigs = new AzureSettings();
                configRoot.Bind("AzureSettings", azConfigs);

                // ECS backend service enforced TLS 1.2 access.
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Create and start the environment-independent service.
                Service.Instance.Initialize(new AzureConfiguration(this.logger, true), this.logger);
                Service.Instance.Start();

                base.OnStart(args);

                this.logger.Info("AudioVideoPlaybackService has been started");
            }
            catch (Exception e)
            {
                this.logger.Error(e, "Exception on AudioVideoPlaybackService startup");
                throw;
            }           
        }

        protected override void OnStop()
        {
            this.logger.Info("AudioVideoPlaybackService is stopping");

            Service.Instance.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            this.logger.Info("AudioVideoPlaybackService has stopped");
        }
    }
}
