using System.Collections.Generic;
using CseSample.Services;
using Microsoft.Graph;
using Moq;
using Xunit;

namespace CseSample.Tests
{
    public class CallServiceTest
    {
        private string[] _userIds = new string[] { "id1", "id2" };
        private string _tenantId = "tenantId";
        private string _accessToken = "accessToken";

        [Fact]
        public async void StartGroupCall_ExpectedInput_ReturnTrue()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(g => g.Communications.Calls.Request(It.IsAny<List<HeaderOption>>()).AddAsync(It.IsAny<Call>()))
                .ReturnsAsync(new Mock<Call>().Object);

            var callServiceMock = new CallService(graphClientMock.Object);

            // Act
            var result = await callServiceMock.StartGroupCallWithSpecificMembers(_userIds, _tenantId, _accessToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async void StartGroupCall_ExpectedInput_ThrowsException()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(g => g.Communications.Calls.Request(It.IsAny<List<HeaderOption>>()).AddAsync(It.IsAny<Call>()))
                .ThrowsAsync(new ServiceException(new Mock<Error>().Object));

            var callServiceMock = new CallService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(async () => await callServiceMock.StartGroupCallWithSpecificMembers(_userIds, _tenantId, _accessToken));
        }

        [Fact]
        public async void JoinExistingOnlineMeeting_ExpectedInput_ReturnTrue()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Communications.Calls.Request(It.IsAny<List<HeaderOption>>()).AddAsync(It.IsAny<Call>()))
                .ReturnsAsync(new Mock<Call>().Object);

            var callService = new CallService(graphClientMock.Object);

            // Act
            var result = await callService.JoinExistingOnlineMeeting("userId", new Meeting(new Attendee[] { }, new OnlineMeetingInfo()), "accessToken");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async void JoinExistingOnlineMeeting_ExpectedInput_MayThrowsServiceException()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Communications.Calls.Request(It.IsAny<List<HeaderOption>>()).AddAsync(It.IsAny<Call>()))
                .ThrowsAsync(new ServiceException(new Mock<Microsoft.Graph.Error>().Object));

            var callService = new CallService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(async () => await callService.JoinExistingOnlineMeeting("userId", new Meeting(new Attendee[] { }, new OnlineMeetingInfo()), "accessToken"));
        }

        [Fact]
        public async void InviteUserToOnlineMeeting_ExpectedInput_ReturnTrue()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Communications.Calls[It.IsAny<string>()].Participants.Invite(It.IsAny<InvitationParticipantInfo[]>(), null).Request(It.IsAny<List<HeaderOption>>()).PostAsync())
                .ReturnsAsync(new Mock<InviteParticipantsOperation>().Object);

            var callService = new CallService(graphClientMock.Object);

            // Act
            var result = await callService.InviteUserToOnlineMeeting("userId", "tenantId", "callId", "accessToken");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async void InviteUserToOnlineMeeting_ExpectedInput_MayThrowsServiceException()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(gc => gc.Communications.Calls[It.IsAny<string>()].Participants.Invite(It.IsAny<InvitationParticipantInfo[]>(), null).Request(It.IsAny<List<HeaderOption>>()).PostAsync())
                .ThrowsAsync(new ServiceException(new Mock<Microsoft.Graph.Error>().Object));

            var callService = new CallService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(async () => await callService.InviteUserToOnlineMeeting("userId", "tenantId", "callId", "accessToken"));
        }
    }
}