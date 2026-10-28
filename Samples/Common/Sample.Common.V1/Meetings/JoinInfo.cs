// <copyright file="JoinInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Meetings
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Graph.Models;

    /// <summary>
    /// Gets the join information.
    /// </summary>
    public class JoinInfo
    {
        /// <summary>
        /// Parse Join URL into its components.
        /// </summary>
        /// <param name="joinURL">Join URL from Team's meeting body.</param>
        /// <returns>Parsed data.</returns>
        public static (ChatInfo, MeetingInfo) ParseJoinURL(string joinURL)
        {
            var decodedURL = WebUtility.UrlDecode(joinURL);

            //// URL being needs to be in this format.
            //// https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={"Tid":"72f988bf-86f1-41af-91ab-2d7cd011db47","Oid":"550fae72-d251-43ec-868c-373732c2704f","MessageId":"1536978844957"}

            //// Short join URL form:
            //// https://<host>/meet/<joinMeetingId>?p=<passcode>
            ////
            //// The host is not fixed - it varies by cloud (for example teams.cloud.microsoft, or
            //// a government cloud host) - so match on the "/meet/<id>" path
            //// shape rather than on a specific domain.
            ////
            //// The short URL carries the joinMeetingId and the passcode directly, so no Microsoft
            //// Graph lookup is required: the service resolves the meeting from the joinMeetingId.
            //// This is parsed from the raw URL rather than from decodedURL, because UrlDecode turns
            //// "+" into a space and "%26" into "&", either of which would corrupt the passcode.
            if (TryParseShortJoinUrl(joinURL, out var joinMeetingIdMeetingInfo))
            {
                //// ChatInfo is deliberately null. A short URL carries no chat thread, and joining by
                //// joinMeetingId takes its coordinates from MeetingInfo alone - JoinMeetingParameters
                //// requires MeetingInfo but accepts a null ChatInfo. Returning a synthetic thread id
                //// instead would make this method non-deterministic, would be sent to the service as
                //// though it were a real thread, and would break callers that key state on ThreadId.
                return (null, joinMeetingIdMeetingInfo);
            }

            var regex = new Regex("https://teams\\.microsoft\\.com.*/(?<thread>[^/]+)/(?<message>[^/]+)\\?context=(?<context>{.*})");
            var match = regex.Match(decodedURL);
            if (!match.Success)
            {
                throw new ArgumentException($"Join URL cannot be parsed: {joinURL}.", nameof(joinURL));
            }

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(match.Groups["context"].Value)))
            {
                var ctxt = (Context)new DataContractJsonSerializer(typeof(Context)).ReadObject(stream);
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

                // meetingInfo.Organizer.User.SetTenantId(ctxt.Tid);
                return (chatInfo, meetingInfo);
            }
        }

        /// <summary>
        /// Tries to parse the short Teams meeting join URL form
        /// <c>https://&lt;host&gt;/meet/&lt;joinMeetingId&gt;?p=&lt;passcode&gt;</c>.
        /// </summary>
        /// <param name="joinURL">The raw, still-encoded join URL.</param>
        /// <param name="meetingInfo">The parsed meeting info, or null when this is not a short join URL.</param>
        /// <returns>True when the URL is a short join URL.</returns>
        private static bool TryParseShortJoinUrl(string joinURL, out JoinMeetingIdMeetingInfo meetingInfo)
        {
            meetingInfo = null;

            if (!Uri.TryCreate(joinURL, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            //// The path must be exactly "/meet/<digits>". Extra or empty path segments (including a
            //// trailing slash or doubled separators) and trailing characters after the digits are
            //// not a short join URL, so they fall through to the long-URL parser and are rejected
            //// instead of being normalized into a plausible-looking meeting id.
            const string ShortJoinPathPrefix = "/meet/";
            var path = uri.AbsolutePath;
            if (!path.StartsWith(ShortJoinPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var joinMeetingId = path.Substring(ShortJoinPathPrefix.Length);
            if (!IsAllDigits(joinMeetingId))
            {
                return false;
            }

            meetingInfo = new JoinMeetingIdMeetingInfo
            {
                JoinMeetingId = joinMeetingId,

                //// Uri.Query excludes any fragment, so "?p=abc#frag" correctly yields "abc".
                Passcode = GetQueryParameter(uri.Query, "p"),
            };

            return true;
        }

        /// <summary>
        /// Determines whether a value is a non-empty run of ASCII digits.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True when the value consists only of ASCII digits.</returns>
        private static bool IsAllDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (var character in value)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a single parameter out of a URL query string.
        /// </summary>
        /// <param name="query">The query component, with or without its leading '?'.</param>
        /// <param name="name">The parameter name to look for.</param>
        /// <returns>The decoded value, or null when the parameter is absent or empty.</returns>
        private static string GetQueryParameter(string query, string name)
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            foreach (var pair in query.TrimStart('?').Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                var separatorIndex = pair.IndexOf('=');
                var key = separatorIndex < 0 ? pair : pair.Substring(0, separatorIndex);

                if (!string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (separatorIndex < 0)
                {
                    return null;
                }

                var value = pair.Substring(separatorIndex + 1);

                //// UnescapeDataString rather than UrlDecode: it does not turn "+" into a space, so a
                //// passcode containing a literal "+" survives intact.
                return value.Length == 0 ? null : Uri.UnescapeDataString(value);
            }

            return null;
        }

        /// <summary>
        /// Join URL context.
        /// </summary>
        [DataContract]
        private class Context
        {
            /// <summary>
            /// Gets or sets the Tenant Id.
            /// </summary>
            [DataMember]
            public string Tid { get; set; }

            /// <summary>
            /// Gets or sets the AAD object id of the user.
            /// </summary>
            [DataMember]
            public string Oid { get; set; }

            /// <summary>
            /// Gets or sets the chat message id.
            /// </summary>
            [DataMember]
            public string MessageId { get; set; }
        }
    }
}
