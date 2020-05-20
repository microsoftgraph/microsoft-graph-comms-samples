using CseSample;
using CseSample.Models;
using CseSample.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MsTeamsCall.Tests
{
    public class CallFunctionTest
    {
        string _accessToken = "dummyAccessToken";
        string[] _userIds = new string[] { "id1", "id2" };

        [Fact]
        public async Task CallFunction_request_returns200OK()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock.Setup(t => t.FetchAccessTokenByTenantId(It.IsAny<string>()))
                .ReturnsAsync(_accessToken);

            var userServiceMock = new Mock<IUsersService>();
            userServiceMock.Setup(u => u.GetUserIdsFromEmailAsync(It.IsAny<string[]>(), _accessToken))
                .ReturnsAsync(_userIds);

            var callServiceMock = new Mock<ICallService>();
            callServiceMock.Setup(c => c.StartGroupCallWithSpecificMembers(_userIds, It.IsAny<string>(), _accessToken))
                .ReturnsAsync(true);


            var callFunction = new CallFunction(tokenServiceMock.Object, userServiceMock.Object, callServiceMock.Object, new Mock<IMeetingService>().Object);

            // Act
            var result = await callFunction.Calls(this.CreateHttpRequest().Object, new Mock<ILogger>().Object);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CallFunction_request_mayThrowException()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock.Setup(t => t.FetchAccessTokenByTenantId(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            var callFunction = new CallFunction(tokenServiceMock.Object, new Mock<IUsersService>().Object, new Mock<ICallService>().Object, new Mock<IMeetingService>().Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await callFunction.Calls(this.CreateHttpRequest().Object, new Mock<ILogger>().Object));
        }

        [Fact]
        public async Task CallFunction_request_returnBadReqeustResult()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            var callFunction = new CallFunction(new Mock<ITokenService>().Object, new Mock<IUsersService>().Object, new Mock<ICallService>().Object, new Mock<IMeetingService>().Object);

            // Act
            var result = await callFunction.Calls(this.UnexpecterdHttpRequest().Object, new Mock<ILogger>().Object);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        private Mock<HttpRequest> CreateHttpRequest()
        {
            var groupCall = new GroupCall();
            groupCall.TenantId = "tenantId";
            groupCall.ParticipantEmails = new string[] { "test@test.com", "test2@test.com" };
            string requestContent = JsonConvert.SerializeObject(groupCall);

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(h => h.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestContent)));

            return httpRequestMock;
        }

        private Mock<HttpRequest> UnexpecterdHttpRequest()
        {
            // Empty request
            var groupCall = new GroupCall();
            string requestContent = JsonConvert.SerializeObject(groupCall);

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.Setup(h => h.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestContent)));

            return httpRequestMock;
        }
    }
}