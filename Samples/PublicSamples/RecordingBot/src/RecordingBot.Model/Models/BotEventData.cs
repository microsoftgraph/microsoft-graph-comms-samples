using Newtonsoft.Json;

namespace RecordingBot.Model.Models
{
    public class BotEventData
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
