// <copyright file="SampleLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// Memory logger for quick diagnostics.
    /// Note: Do not use in production code.
    /// </summary>
    public class SampleLogger : GraphLogger
    {
        /// <summary>
        /// Observer subscription.
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Linked list representing the logs.
        /// </summary>
        private LinkedList<string> logs = new LinkedList<string>();

        /// <summary>
        /// Lock for securing logs.
        /// </summary>
        private object lockLogs = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleLogger"/> class.
        /// </summary>
        /// <param name="component">The component in which log is createdThe component in which this logger is created.</param>
        /// <param name="redirectToTrace">if set to <c>true</c> [redirect to trace].</param>
        public SampleLogger(string component = null, bool redirectToTrace = false)
            : base(component, redirectToTrace: redirectToTrace)
        {
            // Log unhandled exceptions.
            AppDomain.CurrentDomain.UnhandledException += (_, e) => this.Error(e.ExceptionObject as Exception, $"Unhandled exception");
            TaskScheduler.UnobservedTaskException += (_, e) => this.Error(e.Exception, "Unobserved task exception");

            this.subscription = this.CreateObserver(
                this.OnNext,
                null,
                this.Dispose);
        }

        /// <summary>
        /// Get the complete or portion of the logs.
        /// </summary>
        /// <param name="skip">Skip number of entries.</param>
        /// <param name="take">Pagination size.</param>
        /// <returns>Log entries.</returns>
        public string GetLogs(int skip = 0, int take = int.MaxValue)
        {
            lock (this.lockLogs)
            {
                skip = skip < 0 ? Math.Max(0, this.logs.Count + skip) : skip;
                return string.Join(string.Empty, this.logs.Skip(skip).Take(take));
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">Disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Utilities.SafeDispose(ref this.subscription, this);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Handle log event.
        /// </summary>
        /// <param name="logEvent">Log event.</param>
        private void OnNext(LogEvent logEvent)
        {
            var text = $"{logEvent.Timestamp:O}: {logEvent.Level}: {logEvent.Message} {(logEvent.Properties == null ? string.Empty : logEvent.PropertiesString)}\r\n";

            lock (this.lockLogs)
            {
                this.logs.AddFirst(text);
            }
        }
    }
}
