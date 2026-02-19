// <copyright file="JoinInfoTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Samples.Common.Tests.Meetings
{
    using System;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Sample.Common.Meetings;

    /// <summary>
    /// Unit tests for JoinInfo URL parsing.
    /// </summary>
    [TestClass]
    public class JoinInfoTests
    {
        private const string OldFormatUrl = "https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={\"Tid\":\"72f988bf-86f1-41af-91ab-2d7cd011db47\",\"Oid\":\"550fae72-d251-43ec-868c-373732c2704f\",\"MessageId\":\"1536978844957\"}";
        private const string OldFormatUrlEncoded = "https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context=%7B%22Tid%22%3A%2272f988bf-86f1-41af-91ab-2d7cd011db47%22%2C%22Oid%22%3A%22550fae72-d251-43ec-868c-373732c2704f%22%2C%22MessageId%22%3A%221536978844957%22%7D";
        private const string NewFormatUrlShort = "https://teams.microsoft.com/l/meetup-join/19:meeting_abc123def456@thread.v2/0";
        private const string NewFormatUrlWithoutContext = "https://teams.microsoft.com/l/meetup-join/19:meeting_YzY4NDFjZDUtZTdlOC00MDg3LWI3M2QtZTgzYzdiYzM0Yjk5@thread.skype/0";

        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        public TestContext? TestContext { get; set; }

        /// <summary>
        /// Test that old format URL with context parameter parses successfully.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_OldFormatWithContext_ParsesSuccessfully()
        {
            // Act
            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(OldFormatUrl);

            // Assert
            Assert.IsNotNull(chatInfo);
            Assert.IsNotNull(meetingInfo);
            Assert.AreEqual("19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype", chatInfo.ThreadId);
            Assert.AreEqual("1509579179399", chatInfo.MessageId);
            Assert.AreEqual("1536978844957", chatInfo.ReplyChainMessageId);

            Assert.IsInstanceOfType(meetingInfo, typeof(OrganizerMeetingInfo));
            var organizerInfo = (OrganizerMeetingInfo)meetingInfo;
            Assert.IsNotNull(organizerInfo.Organizer);
            Assert.IsNotNull(organizerInfo.Organizer.User);
            Assert.AreEqual("550fae72-d251-43ec-868c-373732c2704f", organizerInfo.Organizer.User.Id);
        }

        /// <summary>
        /// Test that old format URL with encoded context parameter parses successfully.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_OldFormatUrlEncoded_ParsesSuccessfully()
        {
            // Act
            var (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(OldFormatUrlEncoded);

            // Assert
            Assert.IsNotNull(chatInfo);
            Assert.IsNotNull(meetingInfo);
            Assert.AreEqual("19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype", chatInfo.ThreadId);
            Assert.IsInstanceOfType(meetingInfo, typeof(OrganizerMeetingInfo));
        }

        /// <summary>
        /// Test that new shorter URL format throws NotSupportedException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ParseJoinURL_NewShorterFormat_ThrowsNotSupportedException()
        {
            // Act
            JoinInfo.ParseJoinURL(NewFormatUrlShort);
        }

        /// <summary>
        /// Test that new URL format without context throws NotSupportedException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ParseJoinURL_NewFormatWithoutContext_ThrowsNotSupportedException()
        {
            // Act
            JoinInfo.ParseJoinURL(NewFormatUrlWithoutContext);
        }

        /// <summary>
        /// Test that new format URL exception message contains helpful guidance.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_NewFormat_ExceptionContainsGuidance()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<NotSupportedException>(() =>
            {
                JoinInfo.ParseJoinURL(NewFormatUrlShort);
            });

            this.TestContext!.WriteLine($"Exception message: {exception.Message}");

            // Verify the exception message contains helpful guidance
            Assert.IsTrue(exception.Message.Contains("new shorter Teams meeting URL format"));
            Assert.IsTrue(exception.Message.Contains("Graph API"));
            Assert.IsTrue(exception.Message.Contains("JoinMeetingIdMeetingInfo"));
            Assert.IsTrue(exception.Message.Contains("/communications/onlineMeetings"));
        }

        /// <summary>
        /// Test that null URL throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseJoinURL_NullUrl_ThrowsArgumentException()
        {
            // Act
            JoinInfo.ParseJoinURL(null);
        }

        /// <summary>
        /// Test that empty URL throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseJoinURL_EmptyUrl_ThrowsArgumentException()
        {
            // Act
            JoinInfo.ParseJoinURL(string.Empty);
        }

        /// <summary>
        /// Test that whitespace URL throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseJoinURL_WhitespaceUrl_ThrowsArgumentException()
        {
            // Act
            JoinInfo.ParseJoinURL("   ");
        }

        /// <summary>
        /// Test that completely invalid URL throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseJoinURL_InvalidUrl_ThrowsArgumentException()
        {
            // Act
            JoinInfo.ParseJoinURL("https://example.com/not-a-teams-url");
        }

        /// <summary>
        /// Test that malformed Teams URL throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseJoinURL_MalformedTeamsUrl_ThrowsArgumentException()
        {
            // Act
            JoinInfo.ParseJoinURL("https://teams.microsoft.com/malformed?context=invalid");
        }

        /// <summary>
        /// Test that different thread formats are parsed correctly.
        /// </summary>
        [TestMethod]
        public void ParseJoinURL_DifferentThreadFormats_ParsesCorrectly()
        {
            // Arrange
            var threadSkypeUrl = "https://teams.microsoft.com/l/meetup-join/19:meeting_abc@thread.skype/0?context={\"Tid\":\"72f988bf-86f1-41af-91ab-2d7cd011db47\",\"Oid\":\"550fae72-d251-43ec-868c-373732c2704f\"}";
            var threadV2Url = "https://teams.microsoft.com/l/meetup-join/19:meeting_abc@thread.v2/0?context={\"Tid\":\"72f988bf-86f1-41af-91ab-2d7cd011db47\",\"Oid\":\"550fae72-d251-43ec-868c-373732c2704f\"}";

            // Act
            var (chatInfo1, meetingInfo1) = JoinInfo.ParseJoinURL(threadSkypeUrl);
            var (chatInfo2, meetingInfo2) = JoinInfo.ParseJoinURL(threadV2Url);

            // Assert
            Assert.AreEqual("19:meeting_abc@thread.skype", chatInfo1.ThreadId);
            Assert.AreEqual("19:meeting_abc@thread.v2", chatInfo2.ThreadId);
            Assert.IsNotNull(meetingInfo1);
            Assert.IsNotNull(meetingInfo2);
        }
    }
}
