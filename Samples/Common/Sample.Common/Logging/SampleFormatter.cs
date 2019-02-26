// <copyright file="SampleFormatter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Common.Telemetry.HttpLogging;

    /// <summary>
    /// The log event formatter.
    /// </summary>
    /// <seealso cref="ILogEventFormatter" />
    /// <seealso cref="LogEventFormatter" />
    public class SampleFormatter : ILogEventFormatter
    {
        /// <inheritdoc />
        public string Format(LogEvent logEvent)
        {
            var builder = new StringBuilder($"$>{logEvent.Timestamp:O} {logEvent.Level}: {logEvent.CallerInfoString}");
            builder.AppendLine();

            const string correlationKey = "CorrelationId";
            AppendProperty(builder, correlationKey, logEvent.CorrelationId);

            if (logEvent.EventType == LogEventType.HttpTrace)
            {
                var httpLogData = logEvent.GetTypedProperty<HttpLogData>();
                var method = httpLogData.Method;
                var url = httpLogData.Url;
                var headers = httpLogData.Headers.Trim();
                var responseCode = httpLogData.ResponseStatusCode;

                var httpLogDataType = typeof(HttpLogData);
                var ignoreSubProperties = new[]
                {
                    httpLogDataType.GetProperty(nameof(HttpLogData.Method)),
                    httpLogDataType.GetProperty(nameof(HttpLogData.Url)),
                    httpLogDataType.GetProperty(nameof(HttpLogData.Headers)),
                    httpLogDataType.GetProperty(nameof(HttpLogData.ResponseStatusCode)),
                };
                var properties = logEvent.Properties.Flatten(ignoreSubProperties: ignoreSubProperties);
                AppendProperties(builder, properties);

                builder.AppendLine($"request: {method} {url}");

                if (responseCode.HasValue)
                {
                    var response = responseCode.Value >= 100
                        ? $"response: {responseCode.Value} {(HttpStatusCode)responseCode.Value}"
                        : $"response: {responseCode.Value}";
                    builder.AppendLine(response);
                }

                if (!string.IsNullOrWhiteSpace(headers))
                {
                    const string JoinString = "\n  ";
                    headers = string.Join(JoinString, headers.Split('\n').Select(s => s.Trim()));
                    builder.AppendLine($"{nameof(headers)}: {{{JoinString}{headers}\n}}");
                }
            }
            else
            {
                // This should be valid for all event types.
                // Explicitly check for call id.
                const string callKey = "CallId";
                var callId = logEvent.GetTypedProperty<LogProperties.CallData>()?.CallId;
                AppendProperty(builder, callKey, callId);

                // Dump all other properties.
                var properties = logEvent.Properties.Flatten(ignoreTypes: new[] { typeof(LogProperties.CallData) });
                AppendProperties(builder, properties);
            }

            if (!string.IsNullOrWhiteSpace(logEvent.Message))
            {
                builder.AppendLine(logEvent.Message);
            }

            return builder.ToString();
        }

        /// <summary>
        /// append properties to string builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="key">The property key to add.</param>
        /// <param name="value">The property value to add.</param>
        private static void AppendProperty(StringBuilder builder, string key, object value)
        {
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                return;
            }

            if (value is Guid guidValue && guidValue == Guid.Empty)
            {
                return;
            }

            builder.AppendLine($"{key}: {value}");
        }

        /// <summary>
        /// append properties to string builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="properties">The properties to add.</param>
        private static void AppendProperties(StringBuilder builder, IEnumerable<KeyValuePair<string, object>> properties)
        {
            foreach (var property in properties)
            {
                AppendProperty(builder, property.Key, property.Value);
            }
        }
    }
}
