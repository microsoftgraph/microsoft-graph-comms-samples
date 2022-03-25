// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResponseTimeMiddleware.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   ResponseTimeMiddleware logs the request response times for each request issued to the MVC service
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Middleware
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Owin;

    /// <summary>
    /// ResponseTimeMiddleware to log the response times for requests/response.
    /// </summary>
    public class ResponseTimeMiddleware : OwinMiddleware
    {
        private readonly IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseTimeMiddleware"/> class.
        /// </summary>
        /// <param name="next">OwinMiddleware for next in pipeline.</param>
        /// /// <param name="logger">GRaph Logger.</param>
        public ResponseTimeMiddleware(OwinMiddleware next, IGraphLogger logger)
            : base(next)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async override Task Invoke(IOwinContext context)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var startPeriod = stopwatch.Elapsed;
            this.logger.Log(TraceLevel.Info, $"Servicing request with start time {startPeriod}");
            await this.Next.Invoke(context).ConfigureAwait(false);
            stopwatch.Stop();
            this.logger.Log(TraceLevel.Info, $"Serviced request with stop time {stopwatch.Elapsed}");
            this.logger.Log(TraceLevel.Info, $"Service response time: {stopwatch.Elapsed - startPeriod}");
        }
    }
}
