// ***********************************************************************
// Assembly         : EchoBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="ExceptionLogger.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>Defines the ExceptionLogger type.</summary>
// ***********************************************************************-

using Microsoft.Graph.Communications.Common.Telemetry;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace EchoBot.Services.Http
{
    /// <summary>
    /// The exception logger.
    /// </summary>
    public class ExceptionLogger : IExceptionLogger
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger graphLogger;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLogger" /> class.
        /// </summary>
        /// <param name="graphLogger">Graph Logger</param>
        /// <param name="logger">ILogger.</param>
        public ExceptionLogger(IGraphLogger graphLogger, ILogger logger)
        {
            this.graphLogger = graphLogger;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            this.logger.LogError(context.Exception, "Exception processing HTTP request.");
            this.graphLogger.Error(context.Exception, "Exception processing HTTP request.");
            return Task.CompletedTask;
        }
    }
}
