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
        private readonly bool isIncomingMessageHandler;
        private readonly string[] urlIgnorers;
        private readonly IGraphLogger logger;

        public LoggingMessageHandler(bool isIncomingMessageHandler, IGraphLogger logger, string[] urlIgnorers = null)
        {
            this.isIncomingMessageHandler = isIncomingMessageHandler;
            this.logger = logger;
            this.urlIgnorers = urlIgnorers;
        }

        public static string GetHeadersText(HttpHeaders headers)
        {
            if (headers == null || !headers.Any())
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, headers.Select(s => GetHeaderText(s)));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestCid;
            string responseCid;

            requestCid = isIncomingMessageHandler
                ? AdoptScenarioId(request.Headers)
                : SetScenarioId(request.Headers);

            bool ignore = urlIgnorers != null && urlIgnorers.Any(ignorer => request.RequestUri.ToString().Contains(ignorer, StringComparison.OrdinalIgnoreCase));

            if (ignore)
            {
                return await SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var direction = isIncomingMessageHandler
                ? TransactionDirection.Incoming
                : TransactionDirection.Outgoing;

            var requestHeaders = new List<KeyValuePair<string, IEnumerable<string>>>(request.Headers);
            if (request.Content?.Headers?.Any() == true)
            {
                requestHeaders.AddRange(request.Content.Headers);
            }

            logger.LogHttpMessage(
                TraceLevel.Verbose,
                direction,
                HttpTraceType.HttpRequest,
                request.RequestUri.ToString(),
                request.Method.ToString(),
                obfuscatedContent: null,
                headers: requestHeaders);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);

            responseCid = isIncomingMessageHandler
                ? SetScenarioId(response.Headers)
                : AdoptScenarioId(response.Headers);

            WarnIfDifferent(requestCid, responseCid);

            var responseHeaders = new List<KeyValuePair<string, IEnumerable<string>>>(response.Headers);
            if (response.Content?.Headers?.Any() == true)
            {
                responseHeaders.AddRange(response.Content.Headers);
            }

            logger.LogHttpMessage(
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

        private static string GetHeaderText(KeyValuePair<string, IEnumerable<string>> header)
        {
            return $"{header.Key}: {string.Join(",", header.Value)}";
        }

        private string AdoptScenarioId(HttpHeaders headers)
        {
            string scenarioId = null;
            if (headers.TryGetValues(HttpConstants.HeaderNames.ScenarioId, out IEnumerable<string> values) && Guid.TryParse(values.FirstOrDefault(), out Guid scenarioGuid))
            {
                scenarioId = scenarioGuid.ToString();
                logger.CorrelationId = scenarioGuid;
            }
            else if (headers.TryGetValues(HttpConstants.HeaderNames.ChainId, out values) && Guid.TryParse(values.FirstOrDefault(), out scenarioGuid))
            {
                scenarioId = scenarioGuid.ToString();
                logger.CorrelationId = scenarioGuid;
            }

            return scenarioId;
        }

        private string SetScenarioId(HttpHeaders headers)
        {
            Guid scenarioId = logger.CorrelationId;
            if (scenarioId != Guid.Empty)
            {
                headers.Add(HttpConstants.HeaderNames.ScenarioId, scenarioId.ToString());
            }

            return scenarioId.ToString();
        }

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
                logger.Error(e, "Exception occurred when calling SendAsync");
                throw;
            }
        }

        private void WarnIfDifferent(string requestCid, string responseCid)
        {
            if (string.IsNullOrWhiteSpace(requestCid) || string.IsNullOrWhiteSpace(responseCid))
            {
                return;
            }

            if (!string.Equals(requestCid, responseCid))
            {
                logger.Warn($"The scenarioId of the {(isIncomingMessageHandler ? "incoming" : "outgoing")} request, {requestCid}, is different from the outgoing response, {responseCid}.");
            }
        }
    }
}
