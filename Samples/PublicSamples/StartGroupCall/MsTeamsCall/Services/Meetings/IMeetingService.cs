using System.Threading.Tasks;

namespace CseSample.Services
{
    public interface IMeetingService
    {
        Task<Meeting> GetOnlineMeetingInfo(string meetingId, string userId, string accessToken);
    }
}