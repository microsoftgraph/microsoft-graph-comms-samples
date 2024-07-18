using Microsoft.Graph.Contracts;
using Microsoft.Graph.Models;
using RecordingBot.Model.Models;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace RecordingBot.Services.Util
{
    public static partial class JoinInfo
    {
        public static (ChatInfo, MeetingInfo) ParseJoinURL(string joinURL)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(joinURL, nameof(joinURL));

            var decodedURL = WebUtility.UrlDecode(joinURL);

            //// URL being needs to be in this format.
            //// https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={"Tid":"72f988bf-86f1-41af-91ab-2d7cd011db47","Oid":"550fae72-d251-43ec-868c-373732c2704f","MessageId":"1536978844957"}

            var match = UrlFormat().Match(decodedURL);
            if (!match.Success)
            {
                throw new ArgumentException($"Join URL cannot be parsed: {joinURL}", nameof(joinURL));
            }

            Meeting meeting;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(match.Groups["context"].Value)))
            {
                meeting = (Meeting)new DataContractJsonSerializer(typeof(Meeting)).ReadObject(stream);
            }

            if (string.IsNullOrEmpty(meeting.Tid))
            {
                throw new ArgumentException("Join URL is invalid: missing Tid", nameof(joinURL));
            }

            var chatInfo = new ChatInfo
            {
                ThreadId = match.Groups["thread"].Value,
                MessageId = match.Groups["message"].Value,
                ReplyChainMessageId = meeting.MessageId,
            };

            var meetingInfo = new OrganizerMeetingInfo
            {
                Organizer = new IdentitySet
                {
                    User = new Identity { Id = meeting.Oid },
                },
            };
            meetingInfo.Organizer.User.SetTenantId(meeting.Tid);

            return (chatInfo, meetingInfo);
        }

        [GeneratedRegex("https://teams\\.microsoft\\.com.*/(?<thread>[^/]+)/(?<message>[^/]+)\\?context=(?<context>{.*})")]
        private static partial Regex UrlFormat();
    }
}
