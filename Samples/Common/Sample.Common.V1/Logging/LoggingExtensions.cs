// <copyright file="SampleObserver.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Beta.Logging
{
    using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// LoggingExtensions.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logEventLevel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static SeverityLevel ToSeverityLevel(this TraceLevel logEventLevel)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (logEventLevel)
            {
                case TraceLevel.Error:
                    return SeverityLevel.Error;
                case TraceLevel.Warning:
                    return SeverityLevel.Warning;
                case TraceLevel.Info:
                    return SeverityLevel.Information;
                case TraceLevel.Verbose:
                    return SeverityLevel.Verbose;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logEventLevel), logEventLevel, null);
            }
        }
    }
}
