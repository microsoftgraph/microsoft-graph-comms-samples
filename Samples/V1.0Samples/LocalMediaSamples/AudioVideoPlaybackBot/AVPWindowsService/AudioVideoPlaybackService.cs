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

        public AudioVideoPlaybackService()
        {

            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists(SampleConstants.EventLogSource))
            {
                EventLog.CreateEventSource(SampleConstants.EventLogSource, SampleConstants.EventLogType);
            }
            EventLog.WriteEntry(SampleConstants.EventLogSource, "Initializing AudioVideoPlaybackService", EventLogEntryType.Warning);
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry(SampleConstants.EventLogSource, "Starting AudioVideoPlaybackService", EventLogEntryType.Warning);
            try
            {
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
                EventLog.WriteEntry(SampleConstants.EventLogSource, "AudioVideoPlaybackService Service Started", EventLogEntryType.Warning);
                base.OnStart(args);

            }
            catch (Exception e)
            {
                EventLog.WriteEntry(SampleConstants.EventLogSource, $"AudioVideoPlaybackService Exception caught {e.Message}", EventLogEntryType.Error);
                throw;
            }           
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry(SampleConstants.EventLogSource, "AudioVideoPlaybackService Service Stopping", EventLogEntryType.Warning);
            Service.Instance.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();
            EventLog.WriteEntry(SampleConstants.EventLogSource, "AudioVideoPlaybackService Service Stopped", EventLogEntryType.Warning);
            base.OnStop();
        }
    }
}
