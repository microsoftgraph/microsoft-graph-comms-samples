// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Different contexts for which log statements are produced.  Each of these contexts
//   has a corresponding TraceSource entry in the WorkerRole's app.config file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.Common.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Different contexts for which log statements are produced.  Each of these contexts
    /// has a corresponding TraceSource entry in the WorkerRole's app.config file.
    /// </summary>
    public enum LogContext
    {
        /// <summary>
        /// The authentication token.
        /// </summary>
        AuthToken,

        /// <summary>
        /// The front end.
        /// </summary>
        FrontEnd,

        /// <summary>
        /// The media.
        /// </summary>
        Media,
    }

    /// <summary>
    /// Wrapper class for logging.  This class provides a common mechanism for logging throughout the application.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// The trace sources.
        /// </summary>
        private static readonly Dictionary<LogContext, TraceSource> TraceSources = new Dictionary<LogContext, TraceSource>();

        /// <summary>
        /// All the logs.
        /// </summary>
        private static readonly List<string> Logs = new List<string>();

        /// <summary>
        /// Lock the logs list.
        /// </summary>
        private static readonly object LogsLock = new object();

        /// <summary>
        /// Initializes static members of the <see cref="Log"/> class.
        /// </summary>
        static Log()
        {
            foreach (LogContext context in Enum.GetValues(typeof(LogContext)))
            {
                TraceSources[context] = new TraceSource(context.ToString());
            }
        }

        /// <summary>
        /// Gets the logs collected so far.
        /// </summary>
        public static string AllLogs
        {
            get
            {
                lock (Log.LogsLock)
                {
                    return string.Join("\r\n", Log.Logs);
                }
            }
        }

        /// <summary>
        /// Checks if Verbose method is on.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsVerboseOn(LogContext context)
        {
            TraceSource traceSource = TraceSources[context];
            return traceSource.Switch.Level >= SourceLevels.Verbose || traceSource.Switch.Level == SourceLevels.All;
        }

        /// <summary>
        /// Verbose logging of the message.
        /// </summary>
        /// <param name="callerInfo">
        /// The caller Info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Verbose(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Write(TraceEventType.Verbose, callerInfo, context, format, args);
        }

        /// <summary>
        /// Info level logging of the message.
        /// </summary>
        /// <param name="callerInfo">
        /// The caller Info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Info(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Write(TraceEventType.Information, callerInfo, context, format, args);
        }

        /// <summary>
        /// Warning level logging of the message.
        /// </summary>
        /// <param name="callerInfo">
        /// The caller Info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Warning(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Write(TraceEventType.Warning, callerInfo, context, format, args);
        }

        /// <summary>
        /// Error level logging of the message.
        /// </summary>
        /// <param name="callerInfo">
        /// The caller Info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Error(CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            Write(TraceEventType.Error, callerInfo, context, format, args);
        }

        /// <summary>
        /// Flush the log trace sources.
        /// </summary>
        public static void Flush()
        {
            foreach (TraceSource traceSource in TraceSources.Values)
            {
                traceSource.Flush();
            }
        }

        /// <summary>
        /// The write.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <param name="callerInfo">
        /// The caller info.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Write(TraceEventType level, CallerInfo callerInfo, LogContext context, string format, params object[] args)
        {
            try
            {
                string correlationId = CorrelationId.GetCurrentId() ?? "-";
                string callerInfoString = (callerInfo == null) ? "-" : callerInfo.ToString();
                string tracePrefix = "[" + correlationId + " " + callerInfoString + "] ";

                var data = args.Length == 0 ? tracePrefix + format : string.Format(tracePrefix + format, args);
                TraceSources[context].TraceEvent(level, 0, data);

                lock (Log.LogsLock)
                {
                    Log.Logs.Add(data);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in Log.cs" + ex);
            }
        }
    }
}