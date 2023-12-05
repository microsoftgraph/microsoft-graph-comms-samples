using Microsoft.AspNetCore.Http;
using System;

namespace RecordingBot.Model.Extension
{
    //
    // Summary:
    //     Set of extension methods for Microsoft.AspNetCore.Http.HttpRequest.
    public static class HttpRequestExtensions
    {
        private const string UnknownHostName = "UNKNOWN-HOST";

        private const string MultipleHostName = "MULTIPLE-HOST";

        private const string Comma = ",";

        //
        // Summary:
        //     Gets http request Uri from request object.
        //
        // Parameters:
        //   request:
        //     The Microsoft.AspNetCore.Http.HttpRequest.
        //
        // Returns:
        //     A New Uri object representing request Uri.
        public static Uri GetUri(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new ArgumentException("Http request Scheme is not specified");
            }

            return new Uri(request.Scheme 
                + "://" 
                + ((!request.Host.HasValue) 
                ? UnknownHostName 
                : ((request.Host.Value.IndexOf(Comma, StringComparison.Ordinal) > 0) ? MultipleHostName : request.Host.Value)) 
                + (request.PathBase.HasValue ? request.PathBase.Value : string.Empty) 
                + (request.Path.HasValue ? request.Path.Value : string.Empty) 
                + (request.QueryString.HasValue ? request.QueryString.Value : string.Empty));
        }
    }
}
