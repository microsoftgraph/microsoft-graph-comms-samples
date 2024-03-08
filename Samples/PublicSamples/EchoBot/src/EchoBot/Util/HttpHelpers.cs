// ***********************************************************************
// Assembly         : EchoBot.Util
// Author           : bcage29
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="HttpHelpers.cs" company="Microsoft">
//     Copyright ©  2023
// </copyright>
// <summary></summary>
// ***********************************************************************>
using Microsoft.AspNetCore.Http;

namespace EchoBot.Util
{
    public static class HttpHelpers
    {
        public static HttpRequestMessage ToHttpRequestMessage(this HttpRequest req)
            => new HttpRequestMessage()
                .SetMethod(req)
                .SetAbsoluteUri(req)
                .SetHeaders(req)
                .SetContent(req)
                .SetContentType(req);

        private static HttpRequestMessage SetAbsoluteUri(this HttpRequestMessage msg, HttpRequest req)
            => msg.Set(m => m.RequestUri = new UriBuilder
            {
                Scheme = req.Scheme,
                Host = req.Host.Host,
                Port = req.Host.Port.Value,
                Path = req.PathBase.Add(req.Path),
                Query = req.QueryString.ToString()
            }.Uri);

        private static HttpRequestMessage SetMethod(this HttpRequestMessage msg, HttpRequest req)
            => msg.Set(m => m.Method = new HttpMethod(req.Method));

        private static HttpRequestMessage SetHeaders(this HttpRequestMessage msg, HttpRequest req)
            => req.Headers.Aggregate(msg, (acc, h) => acc.Set(m => m.Headers.TryAddWithoutValidation(h.Key, h.Value.AsEnumerable())));

        private static HttpRequestMessage SetContent(this HttpRequestMessage msg, HttpRequest req)
            => msg.Set(m => m.Content = new StreamContent(req.Body));

        private static HttpRequestMessage SetContentType(this HttpRequestMessage msg, HttpRequest req)
            => msg.Set(m => m.Content.Headers.Add("Content-Type", req.ContentType), applyIf: req.Headers.ContainsKey("Content-Type"));

        private static HttpRequestMessage Set(this HttpRequestMessage msg, Action<HttpRequestMessage> config, bool applyIf = true)
        {
            if (applyIf)
            {
                config.Invoke(msg);
            }

            return msg;
        }
    }
}
