// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the ExceptionLogger type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.Common.Logging
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;
    using Microsoft.Graph.Core.Telemetry;

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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// The log async method.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            this.logger.Error(context.Exception, "Exception processing HTTP request.");
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}