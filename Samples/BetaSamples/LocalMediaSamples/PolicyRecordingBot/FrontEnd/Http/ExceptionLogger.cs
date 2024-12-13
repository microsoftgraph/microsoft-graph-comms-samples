// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the ExceptionLogger type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Http
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// The exception logger middleware.
    /// </summary>
    public class ExceptionLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLoggerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Graph logger.</param>
        public ExceptionLoggerMiddleware(RequestDelegate next, IGraphLogger logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception processing HTTP request.");
                throw; // Re-throw the exception after logging it
            }
        }
    }
}
