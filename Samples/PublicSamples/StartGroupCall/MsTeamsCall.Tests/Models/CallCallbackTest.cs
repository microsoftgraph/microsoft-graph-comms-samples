using Xunit;

namespace CseSample.Models
{
    public class CallCallbackTest
    {
        private CallCallback _callCallBack;
        private const string _tenantId = "tenantId";
        private string _callId = "581f0000-7f1f-4821-8691-75770975b1aa";

        public CallCallbackTest()
        {
            _callCallBack = new CallCallback();
            _callCallBack.Value = new Notification[1];

            var notification = new Notification();
            notification.Resource = $"/app/calls/{_callId}";
            notification.ResourceData = new ResourceData()
            {
                MeetingInfo = new MeetingInfo()
                {
                    Organizer = new Organizer()
                    {
                        User = new User()
                        {
                            TenantId = _tenantId
                        }
                    }
                },
                State = "established"
            };

            _callCallBack.Value[0] = notification;
        }

        [Fact]
        public void CallId_ReturnsCallId()
        {
            // Act
            var result = _callCallBack.Value[0].CallId;

            // Assert
            Assert.Equal(_callId, result);
        }

        [Theory]
        [InlineData(_tenantId, true)]
        [InlineData("incorrectId", false)]
        public void IsValidEstablishedNotification_ReturnExpectedBool(string tenantId, bool expected)
        {
            // Act
            var result = _callCallBack.Value[0].IsValidEstablishedNotification(tenantId);

            // Expect
            Assert.Equal(expected, result);
        }
    }
}