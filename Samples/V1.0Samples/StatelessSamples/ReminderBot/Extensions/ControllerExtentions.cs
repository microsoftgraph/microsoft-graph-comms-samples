// <copyright file="ControllerExtentions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.ReminderBot.Extensions
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Graph;

    /// <summary>
    /// The controller exceptions.
    /// </summary>
    public static class ControllerExtentions
    {
        /// <summary>
        /// Convert exception to action result.
        /// </summary>
        /// <param name="controller">The controller.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>The action result.</returns>
        public static IActionResult Exception(this Controller controller, Exception exception)
        {
            IActionResult result;

            if (exception is ServiceException e)
            {
                controller.HttpContext.Response.CopyHeaders(e.ResponseHeaders);

                int statusCode = (int)e.StatusCode;

                result = statusCode >= 300
                    ? controller.StatusCode(statusCode, e.ToString())
                    : controller.StatusCode((int)HttpStatusCode.InternalServerError, e.ToString());
            }
            else
            {
                result = controller.StatusCode((int)HttpStatusCode.InternalServerError, exception.ToString());
            }

            return result;
        }

        /// <summary>
        /// Copy the response headers to controller.HttpContext.Response.
        /// </summary>
        /// <param name="response">The controller.</param>
        /// <param name="headers">The headers.</param>
        private static void CopyHeaders(this HttpResponse response, HttpHeaders headers)
        {
            if (headers == null)
            {
                // do nothing as the source headers are null.
                return;
            }

            foreach (var header in headers)
            {
                var values = header.Value?.ToArray();
                if (values?.Any() == true)
                {
                    response.Headers.Add(header.Key, new StringValues(values));
                }
            }
        }
    }
}
