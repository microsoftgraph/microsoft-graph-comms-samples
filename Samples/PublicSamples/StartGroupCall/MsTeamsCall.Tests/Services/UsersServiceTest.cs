using CseSample.Services;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Moq;
using Xunit;

namespace CseSample.Tests.Services
{
    public class UsersServiceTest
    {
        private string[] _emails = new string[] { "mail1@test.com", "mail2@test.com" };
        private string _accessToken = "accessToken";

        [Fact]
        public async void GetUserIds_ExpectedInput_ReturnIds()
        {
            // Arrange
            Response response1 = new Response();
            response1.Id = 1;
            response1.Status = 200;
            response1.Body = new Body();
            response1.Body.Value = new object[1] { new User() { Id = "id1" } };

            Response response2 = new Response();
            response2.Id = 2;
            response2.Status = 200;
            response2.Body = new Body();
            response2.Body.Value = new object[1] { new User() { Id = "id2" } };

            BatchResponseBody batchResponseBody = new BatchResponseBody();
            batchResponseBody.Responses = new Response[] { response1, response2 };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(batchResponseBody))));
            BatchResponseContent batchContent = new BatchResponseContent(httpResponse);

            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(g => g.Batch.Request(It.IsAny<List<HeaderOption>>()).PostAsync(It.IsAny<BatchRequestContent>()))
                .ReturnsAsync(batchContent);

            var userService = new UsersService(graphClientMock.Object);

            // Act
            var result = await userService.GetUserIdsFromEmailAsync(_emails, _accessToken);

            // Assert
            Assert.Equal(result, new string[] { "id1", "id2" });
        }

        [Fact]
        public async void GetUserIds_ExpectedInput_MayThrowException()
        {
            // Arrange
            var graphClientMock = new Mock<IGraphServiceClient>();
            graphClientMock.Setup(g => g.Batch.Request(It.IsAny<List<HeaderOption>>()).PostAsync(It.IsAny<BatchRequestContent>()))
                .ThrowsAsync(new ServiceException(new Mock<Microsoft.Graph.Error>().Object));

            var userService = new UsersService(graphClientMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ServiceException>(async () => await userService.GetUserIdsFromEmailAsync(_emails, _accessToken));
        }
    }

    public class BatchResponseBody
    {
        [JsonProperty("responses")]
        public Response[] Responses { get; set; }
    }

    public partial class Response
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("headers", NullValueHandling = NullValueHandling.Ignore)]
        public Headers Headers { get; set; }

        [JsonProperty("body")]
        public Body Body { get; set; }
    }

    public partial class Body
    {
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }

        [JsonProperty("@odata.context", NullValueHandling = NullValueHandling.Ignore)]
        public Uri OdataContext { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public object[] Value { get; set; }
    }

    public partial class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public partial class Headers
    {
        [JsonProperty("location")]
        public Uri Location { get; set; }
    }
}