// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Sample.Common.Logging;

    /// <summary>
    /// Main entry.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Memory logger (not to be used for production.)
        /// </summary>
        private static readonly IGraphLogger SampleLogger = new GraphLogger(typeof(Program).Assembly.GetName().Name);
        private static readonly SampleObserver SampleObserver = new SampleObserver(SampleLogger);

        /// <summary>
        /// Observer subscription.
        /// </summary>
        private static IDisposable subscription = SampleLogger.CreateObserver(OnNext);

        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
                ServiceRuntime.RegisterServiceAsync(
                    "HueBotType",
                    context => new HueBot(context, SampleLogger, SampleObserver)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(HueBot).Name);

                // Prevents this host process from terminating so services keeps running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Default log event handler.
        /// </summary>
        /// <param name="logEvent">Log event.</param>
        private static void OnNext(LogEvent logEvent)
        {
            var text = $"{logEvent.Component}({logEvent.CallerInfoString}) {logEvent.Timestamp:O}: {logEvent.Message}, Properties: {logEvent.PropertiesString}";
            ServiceEventSource.Current.Message(text);
        }
    }
}
