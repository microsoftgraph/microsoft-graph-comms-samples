using System.Threading.Tasks;

namespace CseSample.Services
{
    public interface ICallService
    {
        Task<bool> StartGroupCallWithSpecificMembers(string[] userIds, string tenantId, string accessToken);
        Task<bool> JoinExistingOnlineMeeting(string userId, Meeting meetingInfo, string accessToken);
        Task<bool> InviteUserToOnlineMeeting(string userId, string tenantId, string callId, string accessToken);
    }
}