using System;
using System.Linq;
using System.Threading.Tasks;
using CseSample.Utils;
using Microsoft.Graph;

namespace CseSample.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly IGraphServiceClient _graphClient;

        public MeetingService(IGraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<Meeting> GetOnlineMeetingInfo(string meetingId, string userId, string accessToken)
        {
            if (String.IsNullOrEmpty(meetingId)) throw new ArgumentException("meetingId should be valid string");

            try
            {
                var requestHeaders = AuthUtil.CreateRequestHeader(accessToken);
                Event targetEvent = await _graphClient.Users[userId].Calendar.Events[meetingId].Request(requestHeaders).GetAsync();

                if(targetEvent.Attendees == null || targetEvent.Attendees.Count() == 0) throw new ArgumentException("The meeting doesn't have attendee");

                return new Meeting(targetEvent.Attendees.ToArray(), targetEvent.OnlineMeeting);
            }
            catch (ServiceException)
            {
                // MS Graph may throw ServiceException
                throw;
            }
        }
    }
}