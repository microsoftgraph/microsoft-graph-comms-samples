// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
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

namespace RecordingBot.Services.Http
{
    /// <summary>
    /// The exception logger.
    /// </summary>
    public class ExceptionLogger : IExceptionLogger
    {
        /// <summary>
        /// The logger
        /// </summary>
        private IGraphLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLogger" /> class.
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
