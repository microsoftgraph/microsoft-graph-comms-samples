using Microsoft.AspNetCore.Http;
using System;
using System.Buffers;

namespace RecordingBot.Model.Extension
{
    public static class HttpRequestExtensions
    {
        private const string UnknownHostName = "UNKNOWN-HOST";
        private const string MultipleHostName = "MULTIPLE-HOST";
        private static readonly SearchValues<char> CommaSearch = SearchValues.Create([',']);

        public static Uri GetUri(this HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Scheme, nameof(request.Scheme));

            return new Uri(GetUrl(request));
        }

        public static string GetUrl(this HttpRequest request)
        {
            if (request == null)
            {
                return string.Empty;
            }

            if (!request.Host.HasValue)
            {
                return $"{request.Scheme}://{UnknownHostName}{request.Path}{request.QueryString}";
            }
            if (request.Host.Value.AsSpan().ContainsAny(CommaSearch))
            {
                return $"{request.Scheme}://{MultipleHostName}{request.Path}{request.QueryString}";
            }

            return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
        }
    }
}
