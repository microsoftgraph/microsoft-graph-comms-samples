// <copyright file="UserOnlineMeeting.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
namespace Sample.OnlineMeeting
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;

    /// <summary>
    /// Online meeting class to fetch meeting info based of meeting id (ex: vtckey).
    /// </summary>
    public class UserOnlineMeeting
    {
        private Uri graphEndpointUri;
        private IRequestAuthenticationProvider requestAuthenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserOnlineMeeting"/> class.
        /// </summary>
        /// <param name="requestAuthenticationProvider">The request authentication provider.</param>
        /// <param name="graphUri">The graph url.</param>
        public UserOnlineMeeting(IRequestAuthenticationProvider requestAuthenticationProvider, Uri graphUri)
        {
            this.requestAuthenticationProvider = requestAuthenticationProvider;
            this.graphEndpointUri = graphUri;
        }

        /// <summary>
        /// Creates a new online meeting. Meeting organizer would be the oid of the token.
        /// Permissions required : OnlineMeetings.ReadWrite.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier - needed in case of debugging for correlating client side request with server side logs.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<OnlineMeeting> CreateUserMeetingRequestAsync(string tenantId, Guid scenarioId)
        {
            var statelessClient = new GraphServiceClient(
                this.graphEndpointUri.AbsoluteUri,
                this.GetAuthenticationProvider(tenantId, scenarioId));

            var meetingRequest = statelessClient.Me.OnlineMeetings.Request();

            DateTimeOffset startTime = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
            DateTimeOffset endTime = new DateTimeOffset(DateTime.Now.AddMinutes(30), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));

            var onlineMeeting = new OnlineMeeting()
            {
                Subject = "Test User Meeting",

                StartDateTime = startTime,
                EndDateTime = endTime,
            };

            var meeting = await meetingRequest.AddAsync(onlineMeeting).ConfigureAwait(false);

            return meeting;
        }

        /// <summary>
        /// Gets the authentication provider1.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns> Authenticated provider which can be used to authenticate an outbound request.</returns>
        private IAuthenticationProvider GetAuthenticationProvider(string tenantId, Guid scenarioId)
        {
            return new DelegateAuthenticationProvider(async request =>
            {
                request.Headers.Add(HttpConstants.HeaderNames.ScenarioId, scenarioId.ToString());
                request.Headers.Add(HttpConstants.HeaderNames.ClientRequestId, Guid.NewGuid().ToString());

                await this.requestAuthenticationProvider.AuthenticateOutboundRequestAsync(request, tenantId)
                    .ConfigureAwait(false);
            });
        }
    }
}