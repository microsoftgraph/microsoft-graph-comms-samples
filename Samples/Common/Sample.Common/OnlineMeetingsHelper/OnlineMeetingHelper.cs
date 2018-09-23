// <copyright file="OnlineMeetingHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.OnlineMeetings
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.CoreSDK;
    using Microsoft.Graph.StatefulClient.Authentication;

    /// <summary>
    /// Online meeting class to fetch meeting info based of meeting id (ex: vtckey).
    /// </summary>
    public class OnlineMeetingHelper
    {
        private Uri graphEndpointUri;
        private IRequestAuthenticationProvider requestAuthenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineMeetingHelper"/> class.
        /// </summary>
        /// <param name="requestAuthenticationProvider">The request authentication provider.</param>
        /// <param name="graphUri">The graph url.</param>
        public OnlineMeetingHelper(IRequestAuthenticationProvider requestAuthenticationProvider, Uri graphUri)
        {
            this.requestAuthenticationProvider = requestAuthenticationProvider;
            this.graphEndpointUri = graphUri;
        }

        /// <summary>
        /// Gets the online meeting.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="meetingId">The meeting identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<Microsoft.Graph.OnlineMeeting> GetOnlineMeetingAsync(string tenantId, string meetingId, Guid correlationId)
        {
            IAuthenticationProvider GetAuthenticationProvider()
            {
                return new DelegateAuthenticationProvider(async request =>
                {
                    request.Headers.Add(CoreConstants.Headers.ScenarioId, correlationId.ToString());
                    request.Headers.Add(CoreConstants.Headers.ClientRequestId, Guid.NewGuid().ToString());

                    await this.requestAuthenticationProvider.AuthenticateOutboundRequestAsync(request, tenantId)
                        .ConfigureAwait(false);
                });
            }

            var statelessClient = new DefaultContainerClient(this.graphEndpointUri.AbsoluteUri, GetAuthenticationProvider());
            var meetingRequest = statelessClient.App.OnlineMeetings[meetingId].Request();

            var meeting = await meetingRequest.GetAsync().ConfigureAwait(false);

            return meeting;
        }
    }
}