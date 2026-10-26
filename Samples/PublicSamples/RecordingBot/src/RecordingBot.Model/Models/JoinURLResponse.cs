using Newtonsoft.Json;
using System;

namespace RecordingBot.Model.Models
{
    public partial class JoinURLResponse
    {
        [JsonProperty("callId")]
        public object CallId { get; set; }

        [JsonProperty("scenarioId")]
        public Guid ScenarioId { get; set; }

        [JsonProperty("call")]
        public string Call { get; set; }
    }
}
