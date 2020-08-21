using CseSample.Services;
using Microsoft.Graph;
using Moq;
using System;
using Xunit;

namespace CseSample.Tests.Services
{
    public class MeetingTest
    {
        private Attendee[] _attendees = new Attendee[] {
                new Attendee() { EmailAddress = new EmailAddress() { Address = "user1@test.com" } },
                new Attendee() { EmailAddress = new EmailAddress() { Address = "user2@test.com" } }
        };
        OnlineMeetingInfo _onlineMeetinginfo;

        private string _threadId = "19:meeting_NDM5ZTM1MjUtZTViMi00ODRhLTgzMWQtMmVlNmIwNzY2OGY1@thread.v2";
        private string _messageid = "0";
        private string _organizerId = "ea7140cd-bced-4bdf-931b-06cc30891bb8";
        private string _tenantId = "b21a0d16-4e90-4cdb-a05b-ad3846369881";

        public MeetingTest()
        {
            _onlineMeetinginfo = new OnlineMeetingInfo() { JoinUrl = SharedSettings.EncodedMeetingUrlSample };
        }

        [Fact]
        public void Meeting_NoMeetingInfo_SetFalse()
        {
            // Arrange & Act
            Meeting meeting = new Meeting(_attendees, new Mock<OnlineMeetingInfo>().Object);

            // Assert
            Assert.False(meeting.IsOnlineMeetingSet);
        }

        [Fact]
        public void Meeting_FullMeetingInfo_SetTrue()
        {
            // Arrange & Act
            Meeting meeting = new Meeting(_attendees, _onlineMeetinginfo);

            bool isExpectedAttendeeEmailsLength = meeting.AttendeeEmails.Length == 2;
            bool isExpecterdMeetingUrl = !String.IsNullOrEmpty(meeting.MeetingUrl);
            bool isCorrectThreadId = meeting.ThreadId == _threadId;
            bool isCorrectMessageId = meeting.MessageId == _messageid;
            bool isCorrectOrganizerId = meeting.OrganizerId == _organizerId;
            bool isCorrectTenantId = meeting.TenantId == _tenantId;

            Assert.True(meeting.IsOnlineMeetingSet && isExpectedAttendeeEmailsLength && isExpecterdMeetingUrl 
                && isCorrectThreadId && isCorrectMessageId && isCorrectOrganizerId && isCorrectTenantId);
        }
    }
}