// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingMessageHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Helper class to log HTTP requests and responses and to set the CorrelationID based on the X-Microsoft-Skype-Chain-ID header
//   value of incoming HTTP requests from Skype platform.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using Sample.Common.Logging;

    /// <summary>
    /// Helper class to log HTTP requests and responses and to set the CorrelationID based on the X-Microsoft-Skype-Chain-ID header
    /// value of incoming HTTP requests from Skype platform.
    /// </summary>
    internal class LoggingMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// The cid header name.
        /// </summary>
        public const string CidHeaderName = "X-Microsoft-Skype-Chain-ID";

        /// <summary>
        /// Is the message handler an incoming one?.
        /// </summary>
        private readonly bool isIncomingMessageHandler;

        /// <summary>
        /// The log context.
        /// </summary>
        private readonly LogContext logContext;

        /// <summary>
        /// The URL ignorers.
        /// </summary>
        private readonly string[] urlIgnorers;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingMessageHandler"/> class.
        /// Create a new LoggingMessageHandler.
        /// </summary>
        /// <param name="isIncomingMessageHandler">
        /// The is Incoming Message Handler.
        /// </param>
        /// <param name="logContext">
        /// The log Context.
        /// </param>
        /// <param name="urlIgnorers">
        /// The URL Ignorers.
        /// </param>
        public LoggingMessageHandler(bool isIncomingMessageHandler, LogContext logContext, string[] urlIgnorers = null)
        {
            this.isIncomingMessageHandler = isIncomingMessageHandler;
            this.logContext = logContext;
            this.urlIgnorers = urlIgnorers;
        }

        /// <summary>
        /// The get headers text.
        /// </summary>
        /// <param name="headers">
        /// The headers.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
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
        /// The get body text async.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task<string> GetBodyTextAsync(HttpContent content)
        {
            if (content == null)
            {
                return "(empty body)";
            }

            if (content.IsMimeMultipartContent())
            {
                Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);

                if (!stream.CanSeek)
                {
                    return "(cannot log body because HTTP stream cannot seek)";
                }

                StringBuilder multipartBodyBuilder = new StringBuilder();
                MultipartMemoryStreamProvider streamProvider = new MultipartMemoryStreamProvider();
                await content.ReadAsMultipartAsync(streamProvider, (int)stream.Length)
                    .ConfigureAwait(false);

                try
                {
                    foreach (var multipartContent in streamProvider.Contents)
                    {
                        multipartBodyBuilder.AppendLine("-- beginning of multipart content --");

                        // Headers
                        string headerText = GetHeadersText(multipartContent.Headers);
                        multipartBodyBuilder.AppendLine(headerText);

                        // Body of message
                        string multipartBody = await multipartContent.ReadAsStringAsync().ConfigureAwait(false);

                        if (TryFormatJsonBody(multipartBody, out string formattedJsonBody))
                        {
                            multipartBody = formattedJsonBody;
                        }

                        if (string.IsNullOrWhiteSpace(multipartBody))
                        {
                            multipartBodyBuilder.AppendLine("(empty body)");
                        }
                        else
                        {
                            multipartBodyBuilder.AppendLine(multipartBody);
                        }

                        multipartBodyBuilder.AppendLine("-- end of multipart content --");
                    }
                }
                finally
                {
                    // Reset the stream position so consumers of this class can re-read the multipart content.
                    stream.Position = 0;
                }

                return multipartBodyBuilder.ToString();
            }
            else
            {
                string body = await content.ReadAsStringAsync().ConfigureAwait(false);

                if (TryFormatJsonBody(body, out string formattedJsonBody))
                {
                    body = formattedJsonBody;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return "(empty body)";
                }

                return body;
            }
        }

        /// <summary>
        /// Log the request and response.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation Token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string requestCid;
            string responseCid;

            if (this.isIncomingMessageHandler)
            {
                requestCid = AdoptCorrelationId(request.Headers);
            }
            else
            {
                requestCid = SetCorrelationId(request.Headers);
            }

            bool ignore = this.urlIgnorers != null
                          && this.urlIgnorers.Any(
                              ignorer => request.RequestUri.ToString()
                                             .IndexOf(ignorer, StringComparison.OrdinalIgnoreCase) >= 0);

            if (ignore)
            {
                return await this.SendAndLogAsync(request, cancellationToken).ConfigureAwait(false);
            }

            string localMessageId = Guid.NewGuid().ToString();
            string requestUriText = request.RequestUri.ToString();
            string requestHeadersText = GetHeadersText(request.Headers);

            if (request.Content != null)
            {
                requestHeadersText = string.Join(
                    Environment.NewLine,
                    requestHeadersText,
                    GetHeadersText(request.Content.Headers));
            }

            string requestBodyText = await GetBodyTextAsync(request.Content).ConfigureAwait(false);

            Log.Info(
                new CallerInfo(),
                this.logContext,
                "|| correlationId={0} || local.msgid={1} ||{2}{3}:: {4} {5}{6}{7}{8}{9}{10}$$END$$",
                requestCid,
                localMessageId,
                Environment.NewLine,
                this.isIncomingMessageHandler ? "Incoming" : "Outgoing",
                request.Method.ToString(),
                requestUriText,
                Environment.NewLine,
                requestHeadersText,
                Environment.NewLine,
                requestBodyText,
                Environment.NewLine);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Log.Info(
                new CallerInfo(),
                this.logContext,
                "{0} HTTP request with Local id={1} took {2}ms.",
                this.isIncomingMessageHandler ? "Incoming" : "Outgoing",
                localMessageId,
                stopwatch.ElapsedMilliseconds);

            if (this.isIncomingMessageHandler)
            {
                responseCid = SetCorrelationId(response.Headers);
            }
            else
            {
                responseCid = AdoptCorrelationId(response.Headers);
            }

            this.WarnIfDifferent(requestCid, responseCid);

            HttpStatusCode statusCode = response.StatusCode;

            string responseUriText = request.RequestUri.ToString();
            string responseHeadersText = GetHeadersText(response.Headers);

            if (response.Content != null)
            {
                responseHeadersText = string.Join(
                    Environment.NewLine,
                    responseHeadersText,
                    GetHeadersText(response.Content.Headers));
            }

            string responseBodyText = await GetBodyTextAsync(response.Content).ConfigureAwait(false);

            Log.Info(
                new CallerInfo(),
                this.logContext,
                "|| correlationId={0} || statuscode={1} || local.msgid={2} ||{3}Response to {4}:: {5} {6}{7}{8} {9}{10}{11}{12}{13}{14}$$END$$",
                CorrelationId.GetCurrentId(),
                statusCode,
                localMessageId,
                Environment.NewLine,
                this.isIncomingMessageHandler ? "incoming" : "outgoing",
                request.Method.ToString(),
                responseUriText,
                Environment.NewLine,
                ((int)response.StatusCode).ToString(),
                response.StatusCode.ToString(),
                Environment.NewLine,
                responseHeadersText,
                Environment.NewLine,
                responseBodyText,
                Environment.NewLine);

            return response;
        }

        /// <summary>
        /// The get header text.
        /// </summary>
        /// <param name="header">
        /// The header.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetHeaderText(KeyValuePair<string, IEnumerable<string>> header)
        {
            return $"{header.Key}: {string.Join(",", header.Value)}";
        }

        /// <summary>
        /// adopt correlation id.
        /// </summary>
        /// <param name="headers">
        /// The headers.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string AdoptCorrelationId(HttpHeaders headers)
        {
            string correlationId = null;
            if (headers.TryGetValues(CidHeaderName, out IEnumerable<string> correlationIdHeaderValues))
            {
                correlationId = correlationIdHeaderValues.FirstOrDefault();
                CorrelationId.SetCurrentId(correlationId);
            }

            return correlationId;
        }

        /// <summary>
        /// The set correlation id.
        /// </summary>
        /// <param name="headers">
        /// The headers.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string SetCorrelationId(HttpHeaders headers)
        {
            string correlationId = CorrelationId.GetCurrentId();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                headers.Add(CidHeaderName, correlationId);
            }

            return correlationId;
        }

        /// <summary>
        /// The try format JSON body.
        /// </summary>
        /// <param name="body">
        /// The body.
        /// </param>
        /// <param name="jsonBody">
        /// The JSON body.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool TryFormatJsonBody(string body, out string jsonBody)
        {
            jsonBody = null;

            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            try
            {
                object parsedObject = JsonConvert.DeserializeObject(body);

                if (parsedObject == null)
                {
                    return false;
                }

                jsonBody = JsonConvert.SerializeObject(parsedObject, Formatting.Indented);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// The send and log async method.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
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
                Log.Error(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    "Exception occurred when calling SendAsync: {0}",
                    e.ToString());
                throw;
            }
        }

        /// <summary>
        /// The warn if request and response cid are different method.
        /// </summary>
        /// <param name="requestCid">
        /// The request cid.
        /// </param>
        /// <param name="responseCid">
        /// The response cid.
        /// </param>
        private void WarnIfDifferent(string requestCid, string responseCid)
        {
            if (string.IsNullOrWhiteSpace(requestCid) || string.IsNullOrWhiteSpace(responseCid))
            {
                return;
            }

            if (!string.Equals(requestCid, responseCid))
            {
                Log.Warning(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    "The correlationId of the {0} request, {1}, is different from the {2} response, {3}.",
                    this.isIncomingMessageHandler ? "incoming" : "outgoing",
                    requestCid,
                    "outgoing",
                    responseCid);
            }
        }
    }
}