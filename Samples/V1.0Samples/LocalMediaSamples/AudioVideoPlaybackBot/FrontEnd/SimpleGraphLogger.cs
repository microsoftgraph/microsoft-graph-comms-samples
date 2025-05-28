// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleGraphLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   A simple implementation of IGraphLogger that logs to the Windows Event Log
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// A simple implementation of IGraphLogger that logs to the Windows Event Log.
    /// </summary>
    public class SimpleGraphLogger : IGraphLogger
    {
        private readonly string source;
        private readonly Dictionary<Type, object> properties = new Dictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleGraphLogger"/> class.
        /// </summary>
        /// <param name="source">The source name for event log entries.</param>
        public SimpleGraphLogger(string source = "AudioVideoPlaybackService")
        {
            this.source = source;

            // Set ObfuscationConfiguration to null since we can't create it properly
            // The IGraphLogger interface allows for null ObfuscationConfiguration
        }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the diagnostic level.
        /// </summary>
        public TraceLevel DiagnosticLevel { get; set; } = TraceLevel.Verbose;

        /// <summary>
        /// Gets or sets the logical thread identifier.
        /// </summary>
        public uint LogicalThreadId { get; set; } = (uint)new Random().Next(1, int.MaxValue);

        /// <summary>
        /// Gets the obfuscation configuration.
        /// </summary>
        public Microsoft.Graph.Communications.Common.Telemetry.Obfuscation.ObfuscationConfiguration ObfuscationConfiguration { get; } = null;

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyDictionary<Type, object> Properties => this.properties;

        /// <summary>
        /// Gets or sets the request identifier.
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the string properties.
        /// </summary>
        public IDictionary<string, object> StringProperties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a shim logger with the specified name and ID.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>A new instance of the logger.</returns>
        public IGraphLogger CreateShim(string name, Guid id)
        {
            return new SimpleGraphLogger($"{this.source}_{name}_{id}");
        }

        /// <summary>
        /// Creates a shim logger with the specified name and properties.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>A new instance of the logger.</returns>
        public IGraphLogger CreateShim(string name, IDictionary<string, object> properties = null)
        {
            var logger = new SimpleGraphLogger($"{this.source}_{name}");

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    logger.StringProperties[property.Key] = property.Value;
                }
            }

            return logger;
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        public void Debug(string message, object context = null)
        {
            this.LogToEventLog(message, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        public void Error(string message, object context = null)
        {
            this.LogToEventLog(message, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Error(Exception exception, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            this.LogToEventLog($"{formattedMessage} Exception: {exception}", EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs an error message with format.
        /// </summary>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Error(string message, params object[] args)
        {
            this.LogToEventLog(string.Format(message, args), EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        public void Info(string message, object context = null)
        {
            this.LogToEventLog(message, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs an informational message with format.
        /// </summary>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Info(string message, params object[] args)
        {
            this.LogToEventLog(string.Format(message, args), EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a message with the specified trace level.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Log(TraceLevel level, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            var entryType = level switch
            {
                TraceLevel.Error => EventLogEntryType.Error,
                TraceLevel.Warning => EventLogEntryType.Warning,
                _ => EventLogEntryType.Information
            };

            this.LogToEventLog(formattedMessage, entryType);
        }

        /// <summary>
        /// Logs an event with detailed information.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="component">The component.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>A LogEvent object.</returns>
        public LogEvent Log(TraceLevel level, string eventName, string component, Guid correlationId, Guid transactionId, LogEventType eventType, IEnumerable<object> parameters, string memberName, string filePath, int lineNumber)
        {
            var message = $"{eventName} [{eventType}] [{correlationId}] [{transactionId}] [{memberName}:{filePath}:{lineNumber}]";
            if (parameters != null)
            {
                message += $" Parameters: {string.Join(", ", parameters)}";
            }

            var entryType = level switch
            {
                TraceLevel.Error => EventLogEntryType.Error,
                TraceLevel.Warning => EventLogEntryType.Warning,
                _ => EventLogEntryType.Information
            };

            this.LogToEventLog(message, entryType);

            // Return a new LogEvent instance without trying to set properties
            return new LogEvent();
        }

        /// <summary>
        /// Subscribes an observer to log events.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>A disposable object to unsubscribe.</returns>
        public IDisposable Subscribe(IObserver<LogEvent> observer)
        {
            // Simple implementation that doesn't actually do anything
            return new DummyDisposable();
        }

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        public void Verbose(string message, object context = null)
        {
            // For verbose logs, we might want to filter these in production
            this.LogToEventLog(message, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a verbose message with format.
        /// </summary>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Verbose(string message, params object[] args)
        {
            this.LogToEventLog(string.Format(message, args), EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        public void Warning(string message, object context = null)
        {
            this.LogToEventLog(message, EventLogEntryType.Warning);
        }

        /// <summary>
        /// Logs a warning message with format.
        /// </summary>
        /// <param name="message">The message format.</param>
        /// <param name="args">The message arguments.</param>
        public void Warning(string message, params object[] args)
        {
            this.LogToEventLog(string.Format(message, args), EventLogEntryType.Warning);
        }

        /// <summary>
        /// Logs a message to the Windows Event Log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="entryType">The entry type.</param>
        private void LogToEventLog(string message, EventLogEntryType entryType)
        {
            try
            {
                EventLog.WriteEntry(this.source, message, entryType);
            }
            catch (Exception ex)
            {
                // If logging fails, write to console as a fallback
                Console.WriteLine($"Failed to write to event log: {ex.Message}. Original message: {message}");
            }
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