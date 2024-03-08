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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// The exception logger.
    /// </summary>
    public class ExceptionLogger : IExceptionLogger
    {
        private IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLogger"/> class.
        /// </summary>
        /// <param name="logger">Graph logger.</param>
        public ExceptionLogger(IGraphLogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            this.logger.Error(context.Exception, "Exception processing HTTP request.");
            return Task.CompletedTask;
        }
    }
}