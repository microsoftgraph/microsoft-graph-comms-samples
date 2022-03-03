// <copyright file="ControllerExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System.Net.Http;
using System.Threading.Tasks;

namespace EchoBot.Services.Extensions
{
    /// <summary>
    /// The controller exceptions.
    /// </summary>
    public static class ControllerExtensions
    {
        public static async Task<HttpResponseMessage> GetActionResultAsync(HttpRequestMessage request, HttpResponseMessage responseMessage)
        {
            HttpResponseMessage response = null;
            if (responseMessage.Content == null)
            {
                response = request.CreateResponse(responseMessage.StatusCode);
            }
            else
            {
                var responseBody = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = request.CreateResponse(responseMessage.StatusCode, responseBody);
            }

            var responseHeaders = request.Headers;
            if (responseHeaders == null)
            {
                // do nothing as the source headers are null.
            }
            else
            {
                foreach (var header in responseHeaders)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            return response;
        }
    }
}