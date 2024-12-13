// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpConfigurationInitializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Initialize the HttpConfiguration for OWIN
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Http
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using System.Threading.Tasks;

    /// <summary>
    /// Initialize the HttpConfiguration for OWIN.
    /// </summary>
    public class HttpConfigurationInitializer
    {
        /// <summary>
        /// Configuration settings like Authentication, Routes for OWIN.
        /// </summary>
        /// <param name="app">Builder to configure.</param>
        /// <param name="logger">Graph logger.</param>
        public void ConfigureSettings(IApplicationBuilder app, IGraphLogger logger)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            //var exceptionLogger = new ExceptionLoggerMiddleware(logger);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Add custom middleware for logging
            app.UseMiddleware<LoggingMiddleware>(logger, new[] { "/logs" });
        }
    }

    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IGraphLogger _logger;
        private readonly string[] _urlIgnorers;

        public LoggingMiddleware(RequestDelegate next, IGraphLogger logger, string[] urlIgnorers)
        {
            _next = next;
            _logger = logger;
            _urlIgnorers = urlIgnorers;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Implement your logging logic here
            await _next(context);
        }
    }
}
