using System;

namespace CseSample.Models
{
    public class MeetingCall
    {
        public string TenantId { get; set; }
        public string MeetingId { get; set; }
        public string UserEmail { get; set; }

        public bool IsValid()
        {
            if (String.IsNullOrEmpty(TenantId) || String.IsNullOrEmpty(MeetingId) || String.IsNullOrEmpty(UserEmail)) return false;

            return true;
        }
    }

}