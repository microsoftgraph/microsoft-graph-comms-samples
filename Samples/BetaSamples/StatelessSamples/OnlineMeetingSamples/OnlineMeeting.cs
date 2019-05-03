// <copyright file="OnlineMeeting.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.OnlineMeeting
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Core;

    /// <summary>
    /// Online meeting class to fetch meeting info based of meeting id (ex: vtckey).
    /// </summary>
    public class OnlineMeeting
    {
        private Uri graphEndpointUri;
        private IRequestAuthenticationProvider requestAuthenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineMeeting"/> class.
        /// </summary>
        /// <param name="requestAuthenticationProvider">The request authentication provider.</param>
        /// <param name="graphUri">The graph url.</param>
        public OnlineMeeting(IRequestAuthenticationProvider requestAuthenticationProvider, Uri graphUri)
        {
            this.requestAuthenticationProvider = requestAuthenticationProvider;
            this.graphEndpointUri = graphUri;
        }

        /// <summary>
        /// Gets the online meeting.
        /// Permissions required : Either OnlineMeetings.Read.All or OnlineMeetings.ReadWrite.All.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="meetingId">The meeting identifier.</param>
        /// <param name="scenarioId">The scenario identifier - needed in case of debugging for correlating client side request with server side logs.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<Microsoft.Graph.OnlineMeeting> GetOnlineMeetingAsync(string tenantId, string meetingId, Guid scenarioId)
        {
           var statelessClient = new CallsGraphServiceClient(
               this.graphEndpointUri.AbsoluteUri,
               this.GetAuthenticationProvider(tenantId, scenarioId));

           var meetingRequest = statelessClient.App.OnlineMeetings[meetingId].Request();
           var meeting = await meetingRequest.GetAsync().ConfigureAwait(false);

           return meeting;
        }

        /// <summary>
        /// Creates a new adhoc online meeting.
        /// Permissions required : OnlineMeetings.ReadWrite.All.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="organizerId">The meeting organizer identifier.</param>
        /// <param name="scenarioId">The scenario identifier - needed in case of debugging for correlating client side request with server side logs.</param>
        /// <returns> The onlinemeeting. </returns>
        public async Task<Microsoft.Graph.OnlineMeeting> CreateOnlineMeetingAsync(string tenantId, string organizerId, Guid scenarioId)
        {
            var statelessClient = new CallsGraphServiceClient(
                this.graphEndpointUri.AbsoluteUri,
                this.GetAuthenticationProvider(tenantId, scenarioId));

            var meetingRequest = statelessClient.App.OnlineMeetings.Request();

            var onlineMeeting = new Microsoft.Graph.OnlineMeeting()
            {
                MeetingType = MeetingType.MeetNow,
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
                request.Headers.Add(CommsConstants.Headers.ScenarioId, scenarioId.ToString());
                request.Headers.Add(CommsConstants.Headers.ClientRequestId, Guid.NewGuid().ToString());

                await this.requestAuthenticationProvider.AuthenticateOutboundRequestAsync(request, tenantId)
                    .ConfigureAwait(false);
            });
        }
    }
}
