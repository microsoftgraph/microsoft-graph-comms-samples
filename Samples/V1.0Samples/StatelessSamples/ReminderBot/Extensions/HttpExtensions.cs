// <copyright file="HttpExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.ReminderBot.Extensions
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;

    /// <summary>
    /// Extensions for ASP.NET HTTP request and response.
    /// </summary>
    public static class HttpExtensions
    {
        /// <summary>
        /// Creates the request message asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        public static HttpRequestMessage CreateRequestMessage(this HttpRequest request)
        {
            var displayUri = request.GetDisplayUrl();
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(displayUri),
                Method = new HttpMethod(request.Method),
            };

            if (request.ContentLength.HasValue && request.ContentLength.Value > 0)
            {
                httpRequest.Content = new StreamContent(request.Body);
            }

            // Copy headers
            foreach (var header in request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            return httpRequest;
        }

        /// <summary>
        /// Creates the HTTP response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="httpResponse">The HTTP response.</param>
        /// <returns>The populated <see cref="HttpResponse"/>.</returns>
        public static async Task<HttpResponse> CreateHttpResponseAsync(this HttpResponseMessage response, HttpResponse httpResponse)
        {
            httpResponse.StatusCode = (int)response.StatusCode;

            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                await httpResponse.WriteAsync(content).ConfigureAwait(false);
            }

            // Copy headers
            foreach (var header in response.Headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }

            return httpResponse;
        }
    }
}
