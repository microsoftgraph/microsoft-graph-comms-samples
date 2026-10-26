using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using RecordingBot.Model.Models;
using RecordingBot.Services.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingBot.Services.Util
{
    public class CaptureEvents : BufferBase<object>
    {
        private readonly string _path;
        private readonly JsonSerializer _serializer;

        public CaptureEvents(string path)
        {
            _path = path;
            _serializer = new JsonSerializer();
        }

        private async Task SaveJsonFile(Object data, string fileName)
        {
            Directory.CreateDirectory(_path);

            var fullName = Path.Combine(_path, fileName);

            using (var stream = File.CreateText(fullName))
            {
                using (var writer = new JsonTextWriter(stream))
                {
                    writer.Formatting = Formatting.Indented;
                    _serializer.Serialize(writer, data);
                    await writer.FlushAsync();
                }
            }
        }

        private async Task SaveBsonFile(object data, string fileName)
        {
            Directory.CreateDirectory(_path);

            var fullName = Path.Combine(_path, fileName);

            using (var file = File.Create(fullName))
            {
                using (var bson = new BsonDataWriter(file))
                {
                    _serializer.Serialize(bson, data);
                    await bson.FlushAsync();
                }
            }
        }

        private async Task SaveQualityOfExperienceData(SerializableAudioQualityOfExperienceData data)
        {
            await SaveJsonFile(data, $"{data.Id}-AudioQoE.json");
        }

        private async Task SaveAudioMediaBuffer(SerializableAudioMediaBuffer data)
        {
            await SaveBsonFile(data, data.Timestamp.ToString());
        }

        private async Task SaveParticipantEvent(CollectionEventArgs<IParticipant> data)
        {
            var participant = new SerializableParticipantEvent
            {
                AddedResources = new List<IParticipant>(data.AddedResources.Select(addedResource => new SerilizableParticipant(addedResource))),
                RemovedResources = new List<IParticipant>(data.RemovedResources.Select(removedResource => new SerilizableParticipant(removedResource)))
            };

            await SaveJsonFile(participant, $"{DateTime.UtcNow.Ticks}-participant.json");
        }

        private async Task SaveRequests(string data)
        {
            Directory.CreateDirectory(_path);

            var fullName = Path.Combine(_path, $"{DateTime.UtcNow.Ticks}.json");
            await File.AppendAllTextAsync(fullName, data, Encoding.Unicode);
        }

        protected override async Task Process(object data)
        {
            switch (data)
            {
                case string d:
                    await SaveRequests(d);
                    return;
                case CollectionEventArgs<IParticipant> d:
                    await SaveParticipantEvent(d);
                    return;
                case SerializableAudioMediaBuffer d:
                    await SaveAudioMediaBuffer(d);
                    return;
                case SerializableAudioQualityOfExperienceData q:
                    await SaveQualityOfExperienceData(q);
                    return;
                default:
                    return;
            }
        }

        public async Task Finalize()
        {
            // drain the un-processed buffers on this object
            while (Buffer.Count > 0)
            {
                await Task.Delay(200);
            }

            await End();
        }
    }
}
