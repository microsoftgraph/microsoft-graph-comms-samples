using Microsoft.Graph;
using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace CseSample.Services
{
    public class Meeting
    {
        public string[] AttendeeEmails { get; private set; }
        public string MeetingUrl { get; private set; }
        public bool IsOnlineMeetingSet { get; private set; }
        public string ThreadId { get; private set; }
        public string MessageId { get; private set; }
        public string OrganizerId { get; private set; }
        public string TenantId { get; private set; }

        public Meeting(Attendee[] attendees, OnlineMeetingInfo meetingInfo)
        {
            AttendeeEmails = this.GetAttendeeEmails(attendees);

            string meetingUrl = meetingInfo.JoinUrl;
            if (String.IsNullOrEmpty(meetingUrl))
            {
                IsOnlineMeetingSet = false;
            }
            else
            {
                MeetingUrl = meetingUrl;
                IsOnlineMeetingSet = true;
                this.ExtractAndSetOnlineMeetingInfo(meetingUrl);
            }
        }

        private string[] GetAttendeeEmails(Attendee[] attendees)
        {
            return attendees.Select(a => a.EmailAddress.Address).ToArray();
        }

        private void ExtractAndSetOnlineMeetingInfo(string meetingUrl)
        {
            string decodedUrl = WebUtility.UrlDecode(meetingUrl);
            string[] splitedUrl = decodedUrl.Split("/");
            this.ThreadId = splitedUrl[5];

            string[] meetingInfo = splitedUrl[6].Split("?context=");
            this.MessageId = meetingInfo[0];

            var organizerInfo = JsonConvert.DeserializeObject<OrganizerInfo>(meetingInfo[1]);
            this.TenantId = organizerInfo.Tid;            
            this.OrganizerId = organizerInfo.Oid;
        }
    }

    public class OrganizerInfo
    {
        public string Oid { get; set; }
        public string Tid { get; set; }
    }
}