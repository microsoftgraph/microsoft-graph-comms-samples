// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkerRole.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The worker role.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.WorkerRole
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Sample.AudioVideoPlaybackBot.FrontEnd;
    using Sample.Common.Logging;

    /// <summary>
    /// The worker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
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
        /// The run.
        /// </summary>
        public override void Run()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        /// <summary>
        /// The on start.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool OnStart()
        {
            try
            {
                // Wire up exception handling for unhandled exceptions (bugs).
                AppDomain.CurrentDomain.UnhandledException += this.OnAppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;

                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;
                AzureConfiguration.Instance.Initialize();

                // Create and start the environment-independent service.
                Service.Instance.Initialize(AzureConfiguration.Instance);
                Service.Instance.Start();

                bool result = base.OnStart();

                Log.Info(new CallerInfo(), LogContext.FrontEnd, "WorkerRole has been started");

                return result;
            }
            catch (Exception e)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Exception on startup: {0}", e.ToString());
                throw;
            }
        }

        /// <summary>
        /// The on stop.
        /// </summary>
        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole has stopped");
        }

        /// <summary>
        /// The run async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Log UnObservedTaskExceptions.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(
                new CallerInfo(),
                LogContext.FrontEnd,
                "Unobserved task exception: " + e.Exception);
        }

        /// <summary>
        /// Log any unhandled exceptions that are raised in the service.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(
                new CallerInfo(),
                LogContext.FrontEnd,
                "Unhandled exception: " + e.ExceptionObject);

            Log.Flush(); // process may or may not be terminating so flush log just in case.
        }
    }
}