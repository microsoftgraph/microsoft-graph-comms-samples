// <copyright file="CallAffinityMiddleware.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// The call affinity helper class to help re-route calls to specific web instance.
    /// </summary>
    public class CallAffinityMiddleware
    {
        /// <summary>
        /// The name of web instance ID in query string.
        /// </summary>
        public const string WebInstanceIdName = "webInstanceId";

        private const string RouteCounterName = "routeCounter";

        private IGraphLogger graphLogger;

        private ConcurrentDictionary<string, HttpClient> httpClients = new ConcurrentDictionary<string, HttpClient>();

        private RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallAffinityMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next request delegate.</param>
        /// <param name="logger">The logger.</param>
        public CallAffinityMiddleware(RequestDelegate next, IGraphLogger logger)
        {
            this.graphLogger = logger.CreateShim(nameof(CallAffinityMiddleware));

            this.next = next;
        }

        /// <summary>
        /// Get the web instance call back uri.
        /// </summary>
        /// <param name="baseUri">The base uri.</param>
        /// <returns>The uri with current web instance id as query string.</returns>
        public static Uri GetWebInstanceCallbackUri(Uri baseUri)
        {
            return SetQueryString(baseUri, new QueryString("?").Add(WebInstanceIdName, GetCurrentWebInstanceId()));
        }

        /// <summary>
        /// InvokeAsync of call affinity middleware class.
        /// </summary>
        /// <param name="httpContext">The http context.</param>
        /// <returns>The task for await.</returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            var instanceId = GetWebInstanceId(httpContext);

            var routeCounter = GetRouteCounter(httpContext);

            if (IsToOtherWebInstance(instanceId))
            {
                await this.RerouteAsync(instanceId, routeCounter, httpContext).ConfigureAwait(false);
            }
            else
            {
                await this.next.Invoke(httpContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get the current web instance id.
        /// </summary>
        /// <returns>The current web instance id.</returns>
        private static string GetCurrentWebInstanceId()
        {
            return Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local";
        }

        /// <summary>
        /// Check if current web instance is the target.
        /// </summary>
        /// <param name="instanceId">The web instance id.</param>
        /// <returns>flag to incidate if other web instance is the target.</returns>
        private static bool IsToOtherWebInstance(string instanceId)
        {
            return !string.IsNullOrWhiteSpace(instanceId) && instanceId != GetCurrentWebInstanceId();
        }

        /// <summary>
        /// Set the query string.
        /// </summary>
        /// <param name="uri">The base Uri.</param>
        /// <param name="queryString">The query string to add.</param>
        /// <returns>The new Uri with base Uri and query string.</returns>
        private static Uri SetQueryString(Uri uri, QueryString queryString)
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Query = queryString.ToString().TrimStart('?'),
            };

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Get web instance ID.
        /// </summary>
        /// <param name="httpContext">The http context.</param>
        /// <returns>The web instance ID in query string.</returns>
        private static string GetWebInstanceId(HttpContext httpContext)
        {
            return httpContext.Request.Query[WebInstanceIdName].ToString();
        }

        /// <summary>
        /// Get route counter.
        /// </summary>
        /// <param name="httpContext">The http context.</param>
        /// <returns>The route counter in query string. 0 if it is not presented.</returns>
        private static int GetRouteCounter(HttpContext httpContext)
        {
            int.TryParse(httpContext.Request.Query[RouteCounterName], out int routeCounter);

            return routeCounter;
        }

        /// <summary>
        /// Re-route to the right web instance with the ARR affinity feature of Azure web site.
        /// </summary>
        /// <param name="instanceId">The web instance id.</param>
        /// <param name="routeCounter">The route counter.</param>
        /// <param name="httpContext">The http context.</param>
        /// <returns>the task for await.</returns>
        private async Task RerouteAsync(string instanceId, int routeCounter, HttpContext httpContext)
        {
            var uri = new Uri(httpContext.Request.GetEncodedUrl());

            if (routeCounter >= 1)
            {
                // Stop the routing and return NotFound error
                // as the target instanceID can't match the current instance after re-routing once.
                this.graphLogger.Warn($"incorrect re-route uri {uri.ToString()}");

                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;

                return;
            }

            // Create a new callback uri with new routeCounter value.
            var newUri = SetQueryString(uri, httpContext.Request.QueryString.Add(RouteCounterName, (routeCounter + 1).ToString()));

            // Create new request message based on the new uri.
            var newRequestMessage = httpContext.GetHttpRequestMessage();
            newRequestMessage.RequestUri = newUri;

            // Remove the content to make httpClient.SendAsync check that.
            var requestMethod = httpContext.Request.Method.Normalize();
            if (requestMethod != HttpMethods.Post && requestMethod != HttpMethods.Put && requestMethod != HttpMethods.Patch)
            {
                newRequestMessage.Content = null;
            }

            // Get or create a http client for the specific web instance.
            if (!this.httpClients.TryGetValue(instanceId, out HttpClient httpClient))
            {
                var cookieContainer = new CookieContainer();
                var httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
                cookieContainer.Add(newUri, new Cookie("ARRAffinity", instanceId));
                var newHttpClient = new HttpClient(httpClientHandler);

                httpClient = this.httpClients.AddOrUpdate(instanceId, newHttpClient, (k, v) => v);
            }

            var responseMessage = await httpClient.SendAsync(newRequestMessage).ConfigureAwait(false);

            var response = httpContext.Response;

            // set the status code back to HttpContext.Response.
            response.StatusCode = (int)responseMessage.StatusCode;

            // set content back to HttpContext.Response.
            if (responseMessage.Content != null)
            {
                response.ContentType = responseMessage.Content.Headers.ContentType?.ToString();
                response.ContentLength = responseMessage.Content.Headers.ContentLength;

                // writing to response stream should be after all header modification.
                await responseMessage.Content.CopyToAsync(response.Body).ConfigureAwait(false);
            }
        }
    }
}
