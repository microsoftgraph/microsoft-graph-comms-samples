// ***********************************************************************
// Assembly         : EchoBot.Models
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 10-27-2023
// ***********************************************************************
// <copyright file="JoinInfo.cs" company="Microsoft">
//     Copyright ©  2023
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph;
using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace EchoBot.Models
{
    /// <summary>
    /// Gets the join information.
    /// </summary>
    public class JoinInfo
    {
        /// <summary>
        /// Parse Join URL into its components.
        /// NOTE: This method only works with the OLD Teams meeting URL format that includes a context parameter.
        /// For NEW shorter Teams meeting URLs (introduced with MCnumber rollout), you should:
        /// 1. Use the Graph API to query the OnlineMeeting by JoinWebUrl to get meeting details
        /// 2. Use JoinMeetingIdMeetingInfo with the meeting ID and passcode from the API response
        /// Example: var meeting = await graphClient.Communications.OnlineMeetings.Request().Filter($"JoinWebUrl eq '{encodedUrl}'").GetAsync();
        /// </summary>
        /// <param name="joinURL">Join URL from Team's meeting body.</param>
        /// <returns>Parsed data.</returns>
        /// <exception cref="ArgumentException">Join URL cannot be null or empty: {joinURL} - joinURL</exception>
        /// <exception cref="ArgumentException">Join URL cannot be parsed: {joinURL} - joinURL</exception>
        /// <exception cref="NotSupportedException">Join URL is in new shorter format - use Graph API</exception>
        /// <exception cref="ArgumentException">Join URL is invalid: missing Tid - joinURL</exception>
        public static (ChatInfo, MeetingInfo) ParseJoinURL(string joinURL)
        {
            if (string.IsNullOrWhiteSpace(joinURL))
            {
                throw new ArgumentException($"Join URL cannot be null, empty, or whitespace: {joinURL}", nameof(joinURL));
            }

            var decodedURL = WebUtility.UrlDecode(joinURL);

            //// Old URL format with context parameter:
            //// https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={"Tid":"72f988bf-86f1-41af-91ab-2d7cd011db47","Oid":"550fae72-d251-43ec-868c-373732c2704f","MessageId":"1536978844957"}
            //// New shorter URL format (not supported by this parser):
            //// https://teams.microsoft.com/l/meetup-join/...

            var regex = new Regex("https://teams\\.microsoft\\.com.*/(?<thread>[^/]+)/(?<message>[^/]+)\\?context=(?<context>{.*})");
            var match = regex.Match(decodedURL);
            if (!match.Success)
            {
                // Check if this is a new shorter URL format
                if (decodedURL.Contains("teams.microsoft.com") && decodedURL.Contains("/meetup-join/") && !decodedURL.Contains("?context="))
                {
                    throw new NotSupportedException(
                        $"This appears to be a new shorter Teams meeting URL format which is not supported by this parser. " +
                        $"To join meetings with this URL format, please use the Graph API to resolve the meeting details:\n" +
                        $"1. Query: GET /communications/onlineMeetings?$filter=JoinWebUrl eq '{Uri.EscapeDataString(joinURL)}'\n" +
                        $"2. Use JoinMeetingIdMeetingInfo with the meeting.JoinMeetingIdSettings from the response.\n" +
                        $"See: https://learn.microsoft.com/graph/api/resources/joinmeetingidmeetinginfo");
                }
                throw new ArgumentException($"Join URL cannot be parsed: {joinURL}", nameof(joinURL));
            }

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(match.Groups["context"].Value)))
            {
                var ctxt = (Meeting)new DataContractJsonSerializer(typeof(Meeting)).ReadObject(stream);

                if (string.IsNullOrEmpty(ctxt.Tid))
                {
                    throw new ArgumentException("Join URL is invalid: missing Tid", nameof(joinURL));
                }

                var chatInfo = new ChatInfo
                {
                    ThreadId = match.Groups["thread"].Value,
                    MessageId = match.Groups["message"].Value,
                    ReplyChainMessageId = ctxt.MessageId,
                };

                var meetingInfo = new OrganizerMeetingInfo
                {
                    Organizer = new IdentitySet
                    {
                        User = new Identity { Id = ctxt.Oid },
                    },
                };
                meetingInfo.Organizer.User.SetTenantId(ctxt.Tid);

                return (chatInfo, meetingInfo);
            }
        }
    }
}
