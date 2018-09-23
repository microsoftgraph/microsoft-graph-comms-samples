// <copyright file="OnlineMeeting.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.OnlineMeeting
{
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Online meeting class to fetch meeting info based of meeting id (ex: vtckey).
    /// </summary>
    public class OnlineMeeting
    {
        private static string graphEndpointName = "https://graph.microsoft.com/teamsBeta/";
        private IRequestAuthenticationProvider requestAuthenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineMeeting"/> class.
        /// </summary>
        /// <param name="requestAuthenticationProvider">The request authentication provider.</param>
        public OnlineMeeting(IRequestAuthenticationProvider requestAuthenticationProvider)
        {
            this.requestAuthenticationProvider = requestAuthenticationProvider;
        }

        /// <summary>
        /// Gets the online meeting.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="meetingId">The meeting identifier.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<Microsoft.Graph.OnlineMeeting> GetOnlineMeetingAsync(string tenantId, string meetingId)
        {
            IAuthenticationProvider GetAuthenticationProvider()
            {
                return new DelegateAuthenticationProvider(async request =>
                {
                    await this.requestAuthenticationProvider.AuthenticateOutboundRequestAsync(request, tenantId)
                        .ConfigureAwait(false);
                });
            }

            var statelessClient = new DefaultContainerClient(graphEndpointName, GetAuthenticationProvider());
            var meetingRequest = statelessClient.App.OnlineMeetings[meetingId].Request();
            var meeting = await meetingRequest.GetAsync().ConfigureAwait(false);

            return meeting;
        }
    }
}
