using Newtonsoft.Json;

namespace CseSample.Models
{
    public class CallCallback
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("value")]
        public Notification[] Value { get; set; }
    }

    public class Notification
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("changeType")]
        public string ChangeType { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("resourceUrl")]
        public string ResourceUrl { get; set; }

        [JsonProperty("resourceData")]
        public ResourceData ResourceData { get; set; }

        public string CallId
        {
            private set { }
            get
            {
                string[] splitedResource = this.Resource.Split("/");
                return splitedResource[splitedResource.Length - 1];
            }
        }

        public bool IsValidEstablishedNotification(string tenantId)
        {
            return this.ResourceData.State == "established" && this.ResourceData.MeetingInfo.Organizer.User.TenantId == tenantId;
        }
    }

    public class ResourceData
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("direction")]
        public string Direction { get; set; }

        [JsonProperty("meetingInfo")]
        public MeetingInfo MeetingInfo { get; set; }

        [JsonProperty("callChainId")]
        public string CallChainId { get; set; }
    }

    public class MeetingInfo
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("organizer")]
        public Organizer Organizer { get; set; }

        [JsonProperty("allowConversationWithoutHost")]
        public bool AllowConversationWithoutHost { get; set; }
    }

    public class Organizer
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }
    }

    public class User
    {
        [JsonProperty("@odata.type")]
        public string OdataType { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }
    }
}
