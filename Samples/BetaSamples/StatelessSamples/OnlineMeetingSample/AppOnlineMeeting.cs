// <copyright file="AppOnlineMeeting.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
namespace Sample.OnlineMeeting
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;

    /// <summary>
    /// Online meeting class to fetch meeting info based of meeting id (ex: vtckey).
    /// </summary>
    public class AppOnlineMeeting
    {
        private Uri graphEndpointUri;
        private IRequestAuthenticationProvider requestAuthenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppOnlineMeeting"/> class.
        /// </summary>
        /// <param name="requestAuthenticationProvider">The request authentication provider.</param>
        /// <param name="graphUri">The graph url.</param>
        public AppOnlineMeeting(IRequestAuthenticationProvider requestAuthenticationProvider, Uri graphUri)
        {
            this.requestAuthenticationProvider = requestAuthenticationProvider;
            this.graphEndpointUri = graphUri;
        }

        /// <summary>
        /// Gets the online meeting.
        /// Permissions required : Either OnlineMeetings.Read.All or OnlineMeetings.ReadWrite.All.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="vtcId">The vtcid assoiciated with the meeting.</param>
        /// <param name="scenarioId">The scenario identifier - needed in case of debugging for correlating client side request with server side logs.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<OnlineMeeting> GetOnlineMeetingByVtcIdAsync(string tenantId, string vtcId, Guid scenarioId)
        {
           var statelessClient = new GraphServiceClient(
               this.graphEndpointUri.AbsoluteUri,
               this.GetAuthenticationProvider(tenantId, scenarioId));

           var meetingRequestCollection = statelessClient.Communications.OnlineMeetings.Request();
           meetingRequestCollection.Filter($"VideoTeleconferenceId eq '{vtcId}'");

           var meeting = await meetingRequestCollection.GetAsync().ConfigureAwait(false);

           return meeting.First();
        }

        /// <summary>
        /// Creates a new online meeting.
        /// Permissions required : OnlineMeetings.ReadWrite.All.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="organizerId">The meeting organizer identifier.</param>
        /// <param name="scenarioId">The scenario identifier - needed in case of debugging for correlating client side request with server side logs.</param>
        /// <returns> The onlinemeeting. </returns>
        [Obsolete("This way of creating meeting is obsolete. Check CreateUserMeetingRequestAsync for creating meetings.")]
        public async Task<OnlineMeeting> CreateOnlineMeetingAsync(string tenantId, string organizerId, Guid scenarioId)
        {
            var statelessClient = new GraphServiceClient(
                this.graphEndpointUri.AbsoluteUri,
                this.GetAuthenticationProvider(tenantId, scenarioId));

            var meetingRequest = statelessClient.Communications.OnlineMeetings.Request();

            var onlineMeeting = new OnlineMeeting()
            {
                Participants = new MeetingParticipants()
                {
                    Organizer = new MeetingParticipantInfo()
                    {
                        Identity = new IdentitySet()
                        {
                            User = new Identity()
                            {
                                Id = organizerId,
                            },
                        },
                    },
                },
                Subject = "Test meeting.",
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
