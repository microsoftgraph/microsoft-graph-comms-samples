// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="HttpConfigurationInitializer.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>Initialize the HttpConfiguration for OWIN</summary>
// ***********************************************************************-

using Microsoft.Graph.Communications.Common.Telemetry;
using Owin;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace RecordingBot.Services.Http
{
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
        public void ConfigureSettings(IAppBuilder app, IGraphLogger logger)
        {
            HttpConfiguration httpConfig = new HttpConfiguration();
            httpConfig.MapHttpAttributeRoutes();
            httpConfig.MessageHandlers.Add(new LoggingMessageHandler(isIncomingMessageHandler: true, logger: logger, urlIgnorers: new[] { "/logs" }));

            httpConfig.Services.Add(typeof(IExceptionLogger), new ExceptionLogger(logger));

            // TODO: Provide serializer settings hooks
            // httpConfig.Formatters.JsonFormatter.SerializerSettings = RealTimeMediaSerializer.GetSerializerSettings();
            httpConfig.EnsureInitialized();

            // Use the HTTP configuration initialized above
            app.UseWebApi(httpConfig);
        }
    }
}
