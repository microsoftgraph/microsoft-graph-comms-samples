using System.Runtime.Serialization;

namespace RecordingBot.Model.Models
{
    [DataContract]
    public class Meeting
    {
        [DataMember]
        public string Tid { get; set; }

        [DataMember]
        public string Oid { get; set; }

        [DataMember]
        public string MessageId { get; set; }
    }
}
