// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="LoggingMessageHandler.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>Helper class to log HTTP requests and responses and to set the scenario id based on the Scenario-ID or the X-Microsoft-Skype-Chain-ID headers
//   value of incoming HTTP requests from Skype platform.</summary>
// ***********************************************************************-

using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RecordingBot.Services.Http
{
    /// <summary>
    /// Helper class to log HTTP requests and responses and to set the scenario id based on the Scenario-ID or the X-Microsoft-Skype-Chain-ID headers
    /// value of incoming HTTP requests from Skype platform.
    /// </summary>
    internal class LoggingMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Is the message handler an incoming one?.
        /// </summary>
        private readonly bool isIncomingMessageHandler;

        /// <summary>
        /// The URL ignorers.
        /// </summary>
        private readonly string[] urlIgnorers;

        /// <summary>
        /// Graph logger.
        /// </summary>
        private readonly IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingMessageHandler" /> class.
        /// Create a new LoggingMessageHandler.
        /// </summary>
        /// <param name="isIncomingMessageHandler">The is Incoming Message Handler.</param>
        /// <param name="logger">Graph logger.</param>
        /// <param name="urlIgnorers">The URL Ignorers.</param>
        public LoggingMessageHandler(bool isIncomingMessageHandler, IGraphLogger logger, string[] urlIgnorers = null)
        {
            this.isIncomingMessageHandler = isIncomingMessageHandler;
            this.logger = logger;
            this.urlIgnorers = urlIgnorers;
        }

        /// <summary>
        /// The get headers text.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>The <see cref="string" />.</returns>
        public static string GetHeadersText(HttpHeaders headers)
        {
            if (headers == null || !headers.Any())
            {
                return string.Empty;
            }

            List<string> headerTexts = new List<string>();

            foreach (KeyValuePair<string, IEnumerable<string>> h in headers)
            {
                headerTexts.Add(GetHeaderText(h));
            }

            return string.Join(Environment.NewLine, headerTexts);
        }

        /// <summary>
        /// Log the request and response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns>The <see cref="Task" />.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string requestCid;
            string responseCid;

            if (this.isIncomingMessageHandler)
            {
                requestCid = this.AdoptScenarioId(request.Headers);
            }
            else
            {
                requestCid = this.SetScenarioId(request.Headers);
            }

            bool ignore = this.urlIgnorers != null && this.urlIgnorers
                .Any(ignorer => request.RequestUri.ToString().IndexOf(ignorer, StringComparison.OrdinalIgnoreCase) >= 0);

            if (ignore)
            {
                return await this.SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var direction = this.isIncomingMessageHandler
                ? TransactionDirection.Incoming
                : TransactionDirection.Outgoing;

            var requestHeaders = new List<KeyValuePair<string, IEnumerable<string>>>(request.Headers);
            if (request.Content?.Headers?.Any() == true)
            {
                requestHeaders.AddRange(request.Content.Headers);
            }

            this.logger.LogHttpMessage(
                TraceLevel.Verbose,
                direction,
                HttpTraceType.HttpRequest,
                request.RequestUri.ToString(),
                request.Method.ToString(),
                obfuscatedContent: null,
                headers: requestHeaders);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await this.SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);

            if (this.isIncomingMessageHandler)
            {
                responseCid = this.SetScenarioId(response.Headers);
            }
            else
            {
                responseCid = this.AdoptScenarioId(response.Headers);
            }

            this.WarnIfDifferent(requestCid, responseCid);

            var responseHeaders = new List<KeyValuePair<string, IEnumerable<string>>>(response.Headers);
            if (response.Content?.Headers?.Any() == true)
            {
                responseHeaders.AddRange(response.Content.Headers);
            }

            this.logger.LogHttpMessage(
                TraceLevel.Verbose,
                direction,
                HttpTraceType.HttpResponse,
                request.RequestUri.ToString(),
                request.Method.ToString(),
                obfuscatedContent: null,
                headers: responseHeaders,
                responseCode: (int)response.StatusCode,
                responseTime: stopwatch.ElapsedMilliseconds);

            return response;
        }

        /// <summary>
        /// The get header text.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <returns>The <see cref="string" />.</returns>
        private static string GetHeaderText(KeyValuePair<string, IEnumerable<string>> header)
        {
            return $"{header.Key}: {string.Join(",", header.Value)}";
        }

        /// <summary>
        /// adopt scenario identifier.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>The <see cref="string" />.</returns>
        private string AdoptScenarioId(HttpHeaders headers)
        {
            IEnumerable<string> values;
            Guid scenarioGuid;
            string scenarioId = null;
            if (headers.TryGetValues(HttpConstants.HeaderNames.ScenarioId, out values) && Guid.TryParse(values.FirstOrDefault(), out scenarioGuid))
            {
                scenarioId = scenarioGuid.ToString();
                this.logger.CorrelationId = scenarioGuid;
            }
            else if (headers.TryGetValues(HttpConstants.HeaderNames.ChainId, out values) && Guid.TryParse(values.FirstOrDefault(), out scenarioGuid))
            {
                scenarioId = scenarioGuid.ToString();
                this.logger.CorrelationId = scenarioGuid;
            }

            return scenarioId;
        }

        /// <summary>
        /// The set scenario identifier.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>The <see cref="string" />.</returns>
        private string SetScenarioId(HttpHeaders headers)
        {
            Guid scenarioId = this.logger.CorrelationId;
            if (scenarioId != Guid.Empty)
            {
                headers.Add(HttpConstants.HeaderNames.ScenarioId, scenarioId.ToString());
            }

            return scenarioId.ToString();
        }

        /// <summary>
        /// The send and log async method.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task" />.</returns>
        private async Task<HttpResponseMessage> SendAndLogAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.logger.Error(e, "Exception occurred when calling SendAsync");
                throw;
            }
        }

        /// <summary>
        /// The warn if request and response cid are different method.
        /// </summary>
        /// <param name="requestCid">The request cid.</param>
        /// <param name="responseCid">The response cid.</param>
        private void WarnIfDifferent(string requestCid, string responseCid)
        {
            if (string.IsNullOrWhiteSpace(requestCid) || string.IsNullOrWhiteSpace(responseCid))
            {
                return;
            }

            if (!string.Equals(requestCid, responseCid))
            {
                this.logger.Warn($"The scenarioId of the {(this.isIncomingMessageHandler ? "incoming" : "outgoing")} request, {requestCid}, is different from the outgoing response, {responseCid}.");
            }
        }
    }
}
