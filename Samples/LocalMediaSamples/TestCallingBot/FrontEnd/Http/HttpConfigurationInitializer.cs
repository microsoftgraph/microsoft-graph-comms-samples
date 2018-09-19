// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpConfigurationInitializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Initialize the HttpConfiguration for OWIN
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;

    using Owin;

    using Sample.Common.Logging;

    /// <summary>
    /// Initialize the HttpConfiguration for OWIN.
    /// </summary>
    public class HttpConfigurationInitializer
    {
        /// <summary>
        /// Configuration settings like Authentication, Routes for OWIN.
        /// </summary>
        /// <param name="app">Builder to configure.</param>
        public void ConfigureSettings(IAppBuilder app)
        {
            HttpConfiguration httpConfig = new HttpConfiguration();
            httpConfig.MapHttpAttributeRoutes();
            httpConfig.MessageHandlers.Add(
                new LoggingMessageHandler(isIncomingMessageHandler: true, logContext: LogContext.FrontEnd));

            httpConfig.Services.Add(typeof(IExceptionLogger), new Common.Logging.ExceptionLogger());

            // TODO vidommet: Provide serializer settings hooks
            // httpConfig.Formatters.JsonFormatter.SerializerSettings = RealTimeMediaSerializer.GetSerializerSettings();
            httpConfig.EnsureInitialized();

            // Use the HTTP configuration initialized above
            app.UseWebApi(httpConfig);
        }
    }
}