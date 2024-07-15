// <copyright file="SampleObserver.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Beta.Logging
{

    using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

    public static class RequestTelemetryHelper
    {
        private static readonly TelemetryClient _aiClient = new TelemetryClient();
        private static readonly ConcurrentDictionary<string, RequestTelemetry> _notificationRequestTelemetries = new ConcurrentDictionary<string, RequestTelemetry>();
        private static readonly ConcurrentDictionary<string, Stopwatch> _requestTimers = new ConcurrentDictionary<string, Stopwatch>();

        // ReSharper disable InconsistentNaming
        private const string SUCCESS_CODE = "200";
        private const string FAILURE_CODE = "500";
        private const string IsNotificationTempPropertyName = "IsNotification";

        public static readonly string MessageIdHeaderName = "X-Microsoft-Skype-Message-ID";

        public static RequestTelemetry StartNewRequest(string name, DateTimeOffset startTime, Stopwatch requestTimer,
            bool isNotificationRequest, string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));

            var result = new RequestTelemetry { Name = name, Timestamp = startTime, Id = messageId };
            _requestTimers.TryAdd(messageId, requestTimer);
            if (!isNotificationRequest) return result;

            result.Properties.Add(IsNotificationTempPropertyName, "True");
            _notificationRequestTelemetries.TryAdd(messageId, result);
            return result;
        }

        public static void DispatchRequest(RequestTelemetry request, bool success)
        {
            if (_requestTimers.TryGetValue(request.Id, out var stopwatch))
            {
                request.Duration = stopwatch.Elapsed;
            }
            request.Success = success;
            request.ResponseCode = (success) ? SUCCESS_CODE : FAILURE_CODE;

            if (request.Properties.ContainsKey(IsNotificationTempPropertyName)) return;
            Dispatch(request);
        }

        /// <summary>
        /// Time when queued to Graph Comms. SDK's internal queue
        /// </summary>
        public static void OnNotificationQueued(string messageId)
        {
            if (messageId == null) return;

            if (!_notificationRequestTelemetries.TryGetValue(messageId, out var request)) return;
            if (!_requestTimers.TryGetValue(messageId, out var requestTimer)) return;

            if (request.Metrics.ContainsKey("NotificationQueued")) return;
            request.Metrics.Add("NotificationQueued", requestTimer.ElapsedMilliseconds);
        }

        /// <summary>
        /// Time when the notification triggers the event handler (callback) in the application
        /// </summary>
        public static void OnNotificationReceived(string messageId)
        {
            if (messageId == null) return;

            if (!_notificationRequestTelemetries.TryGetValue(messageId, out var request)) return;
            if (!_requestTimers.TryGetValue(messageId, out var requestTimer)) return;

            if (request.Metrics.ContainsKey("NotificationReceived")) return;
            request.Metrics.Add("NotificationReceived", requestTimer.ElapsedMilliseconds);
        }

        /// <summary>
        /// Convenient overload
        /// </summary>
        public static void OnNotificationReceived(IDictionary<string, object> requestHeaders)
        {
            if (requestHeaders != null && requestHeaders.TryGetValue(MessageIdHeaderName,
                    out var idObj) && idObj is string messageId)
            {
                OnNotificationReceived(messageId);
            }
        }

        /// <summary>
        /// Time when all the notification callbacks have completed
        /// </summary>
        /// <param name="messageId">The value in the <see cref="MessageIdHeaderName"/> HTTP header, aka RequestId</param>
        /// <param name="resourcePath">e.g., /communications/calls/6d1f5700-5fc6-415d-b867-4d067ee6bb19/participants</param>
        public static void OnNotificationProcessed(string messageId, string resourcePath)
        {
            if (messageId == null) return;

            if (!_notificationRequestTelemetries.TryGetValue(messageId, out var request)) return;
            if (!_requestTimers.TryGetValue(messageId, out var requestTimer)) return;

            request.Properties.Add("resourcePath", resourcePath);
            if (!request.Metrics.ContainsKey("NotificationProcessed"))
            {
                request.Metrics.Add("NotificationProcessed", requestTimer.ElapsedMilliseconds);
            }

            request.Properties.Remove(IsNotificationTempPropertyName);

            // If DispatchRequest() hasn't been called (with the final HTTP response time and status)
            // then defer sending the telemetry from here. will be sent when DispatchRequest() is called.
            if (!request.Success.HasValue) return;
            Dispatch(request);
        }

        private static void Dispatch(RequestTelemetry request)
        {
            if (!_notificationRequestTelemetries.TryRemove(request.Id, out _)) return;
            _requestTimers.TryRemove(request.Id, out _);
            _aiClient.TrackRequest(request);
        }
    }
}
