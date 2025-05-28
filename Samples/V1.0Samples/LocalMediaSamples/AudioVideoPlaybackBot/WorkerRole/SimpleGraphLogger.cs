// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleGraphLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   A simple implementation of IGraphLogger that doesn't rely on System.Text.Json
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.WorkerRole
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// A simple implementation of IGraphLogger that doesn't rely on System.Text.Json.
    /// </summary>
    public class SimpleGraphLogger : IGraphLogger
    {
        private readonly string component;
        private readonly bool redirectToTrace;
        private readonly Dictionary<Type, object> properties = new Dictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleGraphLogger"/> class.
        /// </summary>
        /// <param name="component">The component name.</param>
        /// <param name="redirectToTrace">Whether to redirect logs to System.Diagnostics.Trace.</param>
        public SimpleGraphLogger(string component, bool redirectToTrace = false)
        {
            this.component = component;
            this.redirectToTrace = redirectToTrace;
        }

        /// <summary>
        /// Gets or sets the diagnostic level for logging.
        /// </summary>
        public TraceLevel DiagnosticLevel { get; set; } = TraceLevel.Verbose;

        /// <summary>
        /// Gets the obfuscation configuration.
        /// </summary>
        public Microsoft.Graph.Communications.Common.Telemetry.Obfuscation.ObfuscationConfiguration ObfuscationConfiguration { get; } = null;

        /// <summary>
        /// Gets or sets the logical thread ID.
        /// </summary>
        public uint LogicalThreadId { get; set; } = (uint)new Random().Next(1, int.MaxValue);

        /// <summary>
        /// Gets or sets the request ID.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the correlation ID.
        /// </summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets the properties dictionary.
        /// </summary>
        public IReadOnlyDictionary<Type, object> Properties => this.properties;

        /// <summary>
        /// Gets the string properties dictionary.
        /// </summary>
        public IDictionary<string, object> StringProperties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Subscribes an observer to log events.
        /// </summary>
        /// <param name="observer">The observer to subscribe.</param>
        /// <returns>An IDisposable that can be used to unsubscribe.</returns>
        public IDisposable Subscribe(IObserver<LogEvent> observer)
        {
            // Simple implementation that doesn't actually do anything
            return new DummyDisposable();
        }

        /// <summary>
        /// Creates a new instance of the logger with the specified component name.
        /// </summary>
        /// <param name="component">The component name.</param>
        /// <param name="properties">Optional properties to include with the logger.</param>
        /// <returns>A new logger instance.</returns>
        public IGraphLogger CreateShim(string component, IDictionary<string, object> properties = null)
        {
            return new SimpleGraphLogger(component, this.redirectToTrace);
        }

        /// <summary>
        /// Logs a message with the specified trace level.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Log(TraceLevel level, string messageFormat, params object[] args)
        {
            var message = string.Format(messageFormat, args);
            var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{this.component}] {message}";
            if (this.redirectToTrace)
            {
                switch (level)
                {
                    case TraceLevel.Error:
                        Trace.TraceError(logMessage);
                        break;
                    case TraceLevel.Warning:
                        Trace.TraceWarning(logMessage);
                        break;
                    case TraceLevel.Info:
                        Trace.TraceInformation(logMessage);
                        break;
                    case TraceLevel.Verbose:
                        Trace.WriteLine(logMessage);
                        break;
                    default:
                        Trace.WriteLine(logMessage);
                        break;
                }
            }
            else
            {
                Console.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Logs a message with detailed parameters.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="component">The component name.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="memberName">The member name.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>A LogEvent object.</returns>
        public LogEvent Log(
            TraceLevel level,
            string eventName,
            string component,
            Guid correlationId,
            Guid transactionId,
            LogEventType eventType,
            IEnumerable<object> parameters,
            string memberName,
            string filePath,
            int lineNumber)
        {
            // Simple implementation that delegates to the other Log method
            var message = $"{eventName} [{eventType}] [{correlationId}] [{transactionId}] [{memberName}:{filePath}:{lineNumber}]";
            if (parameters != null)
            {
                message += $" Parameters: {string.Join(", ", parameters)}";
            }

            this.Log(level, message);

            // Create a minimal LogEvent - the actual implementation may vary
            // depending on the LogEvent class structure
            var logEvent = new LogEvent();

            // Return the created LogEvent
            return logEvent;
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Error(Exception exception, string messageFormat, params object[] args)
        {
            var message = string.Format(messageFormat, args);
            this.Log(TraceLevel.Error, $"{message} Exception: {exception}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Error(string messageFormat, params object[] args)
        {
            this.Log(TraceLevel.Error, messageFormat, args);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Warning(string messageFormat, params object[] args)
        {
            this.Log(TraceLevel.Warning, messageFormat, args);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Info(string messageFormat, params object[] args)
        {
            this.Log(TraceLevel.Info, messageFormat, args);
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="messageFormat">The message format string.</param>
        /// <param name="args">The format arguments.</param>
        public void Verbose(string messageFormat, params object[] args)
        {
            this.Log(TraceLevel.Verbose, messageFormat, args);
        }

        /// <summary>
        /// A dummy implementation of IDisposable that does nothing.
        /// </summary>
        private class DummyDisposable : IDisposable
        {
            /// <summary>
            /// Disposes of the resources used by this class.
            /// </summary>
            public void Dispose()
            {
                // No-op
            }
        }
    }
}