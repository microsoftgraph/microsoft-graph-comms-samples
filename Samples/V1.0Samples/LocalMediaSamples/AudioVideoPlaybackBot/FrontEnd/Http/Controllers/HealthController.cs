// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HealthController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   HealthController retrieves the service health
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Http.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// HealthController serves health status for the service.
    /// </summary>
    public class HealthController : ApiController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController" /> class.
        /// </summary>
        public HealthController()
        {
        }

        /// <summary>
        /// Handle a callback for an incoming call.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpGet]
        [Route("health")]
        public HttpResponseMessage Health()
        {
            EventLog.WriteEntry(SampleConstants.EventLogSource, "Health endpoint called", EventLogEntryType.Information);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("Service is healthy", Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
