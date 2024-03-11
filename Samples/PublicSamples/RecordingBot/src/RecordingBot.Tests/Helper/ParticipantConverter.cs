using Microsoft.Graph.Communications.Calls;
using Newtonsoft.Json;
using RecordingBot.Model.Extension;
using System;

namespace RecordingBot.Tests.Helper
{
    public class ParticipantConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IParticipant);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(ParticipantExtension));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
