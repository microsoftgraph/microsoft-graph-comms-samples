using CseSample.Models;
using Xunit;

namespace CseSample.Tests.Models
{
    public class GroupCallTest
    {
        string _tenantId = "tentnId";
        string[] _participants = new string[] { "test1@test.com", "test2@test.com" };

        [Fact]
        public void IsValid_ValidInput_ReturnTrue()
        {
            var groupCall = new GroupCall();
            groupCall.TenantId = _tenantId;
            groupCall.ParticipantEmails = _participants;

            var result = groupCall.IsValid();

            Assert.True(result);
        }

        [Fact]
        public void IsValid_NoTeantnId_ReturnFalse()
        {
            var groupCall = new GroupCall();
            groupCall.ParticipantEmails = _participants;

            var result = groupCall.IsValid();

            Assert.False(result);
        }

        [Fact]
        public void IsValid_NoParticipants_ReturnFalse()
        {
            var groupCall = new GroupCall();
            groupCall.TenantId = _tenantId;

            var result = groupCall.IsValid();

            Assert.False(result);
        }
    }
}
