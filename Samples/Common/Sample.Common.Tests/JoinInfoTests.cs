// <copyright file="JoinInfoTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Tests
{
    using System;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Sample.Common.Meetings;

    /// <summary>
    /// Tests for <see cref="JoinInfo.ParseJoinURL(string)"/>, covering both the long
    /// "meetup-join" URL and the short "/meet/{joinMeetingId}?p={passcode}" URL.
    /// </summary>
    [TestClass]
    public class JoinInfoTests
    {
        private const string LongJoinUrl =
            "https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399" +
            "?context={\"Tid\":\"72f988bf-86f1-41af-91ab-2d7cd011db47\",\"Oid\":\"550fae72-d251-43ec-868c-373732c2704f\",\"MessageId\":\"1536978844957\"}";

        /// <summary>
        /// A short URL carries the joinMeetingId in the path and the passcode in "p".
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrlWithPasscode_ReturnsJoinMeetingIdMeetingInfo()
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/meet/123456789012?p=SamplePasscode1234");

            var joinMeetingIdMeetingInfo = meetingInfo as JoinMeetingIdMeetingInfo;
            Assert.IsNotNull(joinMeetingIdMeetingInfo, "Expected a JoinMeetingIdMeetingInfo for a short join URL.");
            Assert.AreEqual("123456789012", joinMeetingIdMeetingInfo.JoinMeetingId);
            Assert.AreEqual("SamplePasscode1234", joinMeetingIdMeetingInfo.Passcode);
        }

        /// <summary>
        /// The short URL carries no chat thread, so ChatInfo must be null rather than a synthetic
        /// thread id. Callers key state on ThreadId, and a fabricated value would also be sent to
        /// the service as though it were a real thread.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrl_ReturnsNullChatInfo()
        {
            var (chatInfo, _) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/meet/123456789012?p=SamplePasscode1234");

            Assert.IsNull(chatInfo, "The short URL path must not fabricate a ChatInfo.");
        }

        /// <summary>
        /// Parsing must be deterministic: the same URL must always produce the same result.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrl_IsDeterministic()
        {
            const string JoinUrl = "https://teams.microsoft.com/meet/123456789012?p=SamplePasscode1234";

            var (firstChatInfo, firstMeetingInfo) = JoinInfo.ParseJoinURL(JoinUrl);
            var (secondChatInfo, secondMeetingInfo) = JoinInfo.ParseJoinURL(JoinUrl);

            Assert.AreEqual(
                ((JoinMeetingIdMeetingInfo)firstMeetingInfo).JoinMeetingId,
                ((JoinMeetingIdMeetingInfo)secondMeetingInfo).JoinMeetingId);
            Assert.AreEqual(
                ((JoinMeetingIdMeetingInfo)firstMeetingInfo).Passcode,
                ((JoinMeetingIdMeetingInfo)secondMeetingInfo).Passcode);
            Assert.AreEqual(firstChatInfo, secondChatInfo);
        }

        /// <summary>
        /// A meeting without a passcode produces a short URL with no "p" parameter.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrlWithoutPasscode_ReturnsNullPasscode()
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/meet/111122223333");

            var joinMeetingIdMeetingInfo = meetingInfo as JoinMeetingIdMeetingInfo;
            Assert.IsNotNull(joinMeetingIdMeetingInfo);
            Assert.AreEqual("111122223333", joinMeetingIdMeetingInfo.JoinMeetingId);
            Assert.IsNull(joinMeetingIdMeetingInfo.Passcode);
        }

        /// <summary>
        /// The short URL host varies by cloud, so parsing must not be
        /// tied to teams.microsoft.com.
        /// </summary>
        /// <param name="joinUrl">The short join URL under test.</param>
        [DataTestMethod]
        [DataRow("https://teams.cloud.microsoft/meet/210987654321?p=SamplePasscode5678")]
        [DataRow("https://teams.microsoft.us/meet/210987654321?p=SamplePasscode5678")]
        public void ParseJoinURL_ShortUrlOnAlternateHost_IsStillParsed(string joinUrl)
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL(joinUrl);

            var joinMeetingIdMeetingInfo = meetingInfo as JoinMeetingIdMeetingInfo;
            Assert.IsNotNull(joinMeetingIdMeetingInfo, "Short URL parsing must not be tied to a specific host.");
            Assert.AreEqual("210987654321", joinMeetingIdMeetingInfo.JoinMeetingId);
            Assert.AreEqual("SamplePasscode5678", joinMeetingIdMeetingInfo.Passcode);
        }

        /// <summary>
        /// The host is intentionally unconstrained because it varies by cloud. This test documents
        /// that deliberate looseness so a future change to it is a conscious decision.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrlShapeOnAnyHost_IsAccepted()
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL("https://contoso.example.com/meet/12345");

            Assert.IsInstanceOfType(
                meetingInfo,
                typeof(JoinMeetingIdMeetingInfo),
                "Host matching is intentionally loose so sovereign and cloud.microsoft hosts keep working.");
        }

        /// <summary>
        /// A passcode may contain characters that URL decoding would otherwise mangle. "+" must not
        /// become a space, and an encoded "&amp;" must not terminate the value.
        /// </summary>
        /// <param name="joinUrl">The short join URL under test.</param>
        /// <param name="expectedPasscode">The passcode that must be recovered.</param>
        [DataTestMethod]
        [DataRow("https://teams.microsoft.com/meet/123456789012?p=aB%2BcD%2BeF", "aB+cD+eF")]
        [DataRow("https://teams.microsoft.com/meet/123456789012?p=aB%26cD", "aB&cD")]
        [DataRow("https://teams.microsoft.com/meet/123456789012?p=aB+cD", "aB+cD")]
        public void ParseJoinURL_ShortUrlWithEncodedPasscode_PreservesPasscode(string joinUrl, string expectedPasscode)
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL(joinUrl);

            Assert.AreEqual(expectedPasscode, ((JoinMeetingIdMeetingInfo)meetingInfo).Passcode);
        }

        /// <summary>
        /// A URL fragment must not be captured into the passcode.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrlWithFragment_ExcludesFragmentFromPasscode()
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/meet/123456789012?p=ABC123#fragment");

            Assert.AreEqual("ABC123", ((JoinMeetingIdMeetingInfo)meetingInfo).Passcode);
        }

        /// <summary>
        /// The passcode must be read from the "p" parameter itself, not from a "p=" sequence that
        /// happens to appear inside another parameter's value.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_ShortUrlWithNestedUrlParameter_DoesNotTakePasscodeFromIt()
        {
            var (_, meetingInfo) = JoinInfo.ParseJoinURL("https://teams.microsoft.com/meet/123456789012?redirect=https%3A%2F%2Fx%2Fy%3Fp%3DWRONG");

            Assert.IsNull(
                ((JoinMeetingIdMeetingInfo)meetingInfo).Passcode,
                "A 'p=' inside another parameter's value must not be treated as the passcode.");
        }

        /// <summary>
        /// A malformed meeting id must be rejected rather than silently truncated to the leading
        /// digits, which would otherwise produce a plausible but wrong meeting id. Extra or empty
        /// path segments must likewise be rejected rather than collapsed into "/meet/{id}".
        /// </summary>
        /// <param name="joinUrl">The malformed short join URL under test.</param>
        [DataTestMethod]
        [DataRow("https://teams.microsoft.com/meet/123456789012abc")]
        [DataRow("https://teams.microsoft.com/meet/abc123456789012")]
        [DataRow("https://teams.microsoft.com/meet/")]
        [DataRow("https://teams.microsoft.com/meet/123456789012/456?p=abc")]
        [DataRow("https://teams.microsoft.com/meet/123456789012/?p=abc")]
        [DataRow("https://teams.microsoft.com/meet//123456789012?p=abc")]
        [DataRow("https://teams.microsoft.com//meet/123456789012?p=abc")]
        public void ParseJoinURL_MalformedShortUrl_Throws(string joinUrl)
        {
            Assert.ThrowsException<ArgumentException>(() => JoinInfo.ParseJoinURL(joinUrl));
        }

        /// <summary>
        /// Only https is accepted, matching the previous behaviour of the long-URL parser.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_NonHttpsShortUrl_Throws()
        {
            Assert.ThrowsException<ArgumentException>(
                () => JoinInfo.ParseJoinURL("http://teams.microsoft.com/meet/123456789012?p=ABC123"));
        }

        /// <summary>
        /// The long URL path must keep returning organizer coordinates and the real chat thread.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_LongUrl_StillReturnsOrganizerMeetingInfo()
        {
            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(LongJoinUrl);

            var organizerMeetingInfo = meetingInfo as OrganizerMeetingInfo;
            Assert.IsNotNull(organizerMeetingInfo);
            Assert.AreEqual("550fae72-d251-43ec-868c-373732c2704f", organizerMeetingInfo.Organizer.User.Id);

            Assert.IsNotNull(chatInfo);
            Assert.AreEqual("19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype", chatInfo.ThreadId);
            Assert.AreEqual("1509579179399", chatInfo.MessageId);
            Assert.AreEqual("1536978844957", chatInfo.ReplyChainMessageId);
        }

        /// <summary>
        /// Anything that is neither a long nor a short join URL is still rejected.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_UnrecognizedUrl_Throws()
        {
            Assert.ThrowsException<ArgumentException>(
                () => JoinInfo.ParseJoinURL("https://contoso.example.com/not-a-meeting"));
        }
    }
}
