using CseSample.Services;
using FluentAssertions;
using System.Collections.Generic;
using Microsoft.Graph;
using Moq;
using Xunit;
using System;

namespace CseSample.Tests.Services
{
    public class MeetingServiceTest
    {
        [Fact]
        public async void GetOnlineMeeting_expectedInput_ReturnMsGraphEvent()
        {
            // Arrange
            var eventWithAttendee = new Event();
            var attendees = new Attendee[] {
                new Attendee() { EmailAddress = new EmailAddress() { Address = "test1@sample.com" } }
            };
            eventWithAttendee.Attendees = attendees;
            var onlineMeetingInfo = new OnlineMeetingInfo() { JoinUrl = SharedSettings.EncodedMeetingUrlSample };
            eventWithAttendee.OnlineMeeting = onlineMeetingInfo;

            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Users[It.IsAny<string>()].Calendar.Events[It.IsAny<string>()].Request(It.IsAny<List<HeaderOption>>()).GetAsync())
                .ReturnsAsync(eventWithAttendee);
            var meetingService = new MeetingService(graphClientMock.Object);

            var expected = new Meeting(attendees, onlineMeetingInfo);

            // Act
            var result = await meetingService.GetOnlineMeetingInfo("meetingId", "userId", "accessToken");

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async void GetOnlineMeeting_NoMeetingId_ThrowsArgumentException()
        {
            // Arrange
            var meetingService = new MeetingService(new Mock<IGraphServiceClient>().Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await meetingService.GetOnlineMeetingInfo(null, "userId", "accessToken"));
        }

        [Fact]
        public async void GetOnlineMeeting_NoAttendeeMeeting_ThrowsArgumentException()
        {
            // Arrange
            var eventWithAttendee = new Event();
            var onlineMeetingInfo = new OnlineMeetingInfo() { JoinUrl = SharedSettings.EncodedMeetingUrlSample };
            eventWithAttendee.OnlineMeeting = onlineMeetingInfo;

            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Users[It.IsAny<string>()].Calendar.Events[It.IsAny<string>()].Request(It.IsAny<List<HeaderOption>>()).GetAsync())
                .ReturnsAsync(eventWithAttendee);
            var meetingService = new MeetingService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await meetingService.GetOnlineMeetingInfo("meetingId", "userId", "accessToken"));
        }

        [Fact]
        public async void GetOnlineMeeting_MayThrowsServiceException()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Users[It.IsAny<string>()].Calendar.Events[It.IsAny<string>()].Request(It.IsAny<List<HeaderOption>>()).GetAsync())
                .ThrowsAsync(new ServiceException(new Mock<Microsoft.Graph.Error>().Object));
            var meetingService = new MeetingService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(async () => await meetingService.GetOnlineMeetingInfo("meetingId", "userId", "accessToken"));
        }
    }
}