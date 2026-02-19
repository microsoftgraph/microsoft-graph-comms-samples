// <copyright file="JoinInfoHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Meetings
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Graph;

    /// <summary>
    /// Helper class for handling both old and new Teams meeting URL formats.
    /// </summary>
    public class JoinInfoHelper
    {
        private readonly GraphServiceClient graphClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinInfoHelper"/> class.
        /// </summary>
        /// <param name="graphClient">The Graph service client.</param>
        public JoinInfoHelper(GraphServiceClient graphClient)
        {
            this.graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        }

        /// <summary>
        /// Gets meeting information from a join URL, supporting both old and new URL formats.
        /// For new shorter URLs, this method queries the Graph API to resolve meeting details.
        /// </summary>
        /// <param name="joinUrl">The Teams meeting join URL.</param>
        /// <returns>A tuple containing ChatInfo and MeetingInfo.</returns>
        public async Task<(ChatInfo ChatInfo, MeetingInfo MeetingInfo)> GetMeetingInfoAsync(string joinUrl)
        {
            if (string.IsNullOrWhiteSpace(joinUrl))
            {
                throw new ArgumentException("Join URL cannot be null or empty.", nameof(joinUrl));
            }

            // Try parsing the old format first
            try
            {
                return JoinInfo.ParseJoinURL(joinUrl);
            }
            catch (NotSupportedException)
            {
                // This is a new shorter URL format - use Graph API to resolve it
                return await this.ResolveNewFormatUrlAsync(joinUrl).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Resolves a new-format Teams meeting URL using the Graph API.
        /// </summary>
        /// <param name="joinUrl">The Teams meeting join URL.</param>
        /// <returns>A tuple containing ChatInfo and MeetingInfo.</returns>
        private async Task<(ChatInfo ChatInfo, MeetingInfo MeetingInfo)> ResolveNewFormatUrlAsync(string joinUrl)
        {
            // Query the Graph API to get the meeting details
            var encodedUrl = HttpUtility.UrlEncode(joinUrl);
            var filter = $"JoinWebUrl eq '{encodedUrl}'";

            var meetings = await this.graphClient.Communications.OnlineMeetings
                .Request()
                .Filter(filter)
                .GetAsync()
                .ConfigureAwait(false);

            if (meetings == null || meetings.Count == 0)
            {
                throw new InvalidOperationException($"No meeting found for URL: {joinUrl}");
            }

            var meeting = meetings[0];

            // Build ChatInfo from the meeting
            var chatInfo = new ChatInfo
            {
                ThreadId = meeting.ChatInfo?.ThreadId,
                MessageId = meeting.ChatInfo?.MessageId,
                ReplyChainMessageId = meeting.ChatInfo?.ReplyChainMessageId,
            };

            // Build MeetingInfo using JoinMeetingIdSettings if available
            MeetingInfo meetingInfo;
            if (meeting.JoinMeetingIdSettings != null && !string.IsNullOrEmpty(meeting.JoinMeetingIdSettings.JoinMeetingId))
            {
                meetingInfo = new JoinMeetingIdMeetingInfo
                {
                    JoinMeetingId = meeting.JoinMeetingIdSettings.JoinMeetingId,
                    Passcode = meeting.JoinMeetingIdSettings.Passcode,
                };
            }
            else if (meeting.Participants?.Organizer?.Identity?.User != null)
            {
                // Fall back to OrganizerMeetingInfo if available
                meetingInfo = new OrganizerMeetingInfo
                {
                    Organizer = new IdentitySet
                    {
                        User = meeting.Participants.Organizer.Identity.User,
                    },
                };
            }
            else
            {
                throw new InvalidOperationException($"Unable to construct MeetingInfo from meeting response for URL: {joinUrl}");
            }

            return (chatInfo, meetingInfo);
        }

        /// <summary>
        /// Example: Creating a JoinMeetingParameters object for joining a meeting.
        /// </summary>
        /// <param name="joinUrl">The Teams meeting join URL.</param>
        /// <param name="mediaSession">The local media session.</param>
        /// <param name="tenantId">Optional tenant ID (required for OrganizerMeetingInfo).</param>
        /// <returns>A JoinMeetingParameters object ready to use with client.Calls().AddAsync().</returns>
        public async Task<JoinMeetingParameters> CreateJoinParametersAsync(
            string joinUrl,
            ILocalMediaSession mediaSession,
            string tenantId = null)
        {
            var (chatInfo, meetingInfo) = await this.GetMeetingInfoAsync(joinUrl).ConfigureAwait(false);

            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession);

            // Set tenant ID if using OrganizerMeetingInfo
            if (meetingInfo is OrganizerMeetingInfo && !string.IsNullOrEmpty(tenantId))
            {
                joinParams.TenantId = tenantId;
            }

            return joinParams;
        }
    }
}
