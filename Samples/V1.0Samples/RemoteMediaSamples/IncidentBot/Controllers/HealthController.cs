// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HealthController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   HealthController retrieves the service health
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.IncidentBot.Controllers
{
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using Microsoft.AspNetCore.Mvc;

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
        [Route(HttpRouteConstants.HealthRoute)]
        public HttpResponseMessage Health()
        {
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
