using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using RecordingBot.Model.Models;
using RecordingBot.Services.Media;
using System;
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

            using var stream = File.Open(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var sw = new StreamWriter(stream);
            using var jw = new JsonTextWriter(sw);
            jw.Formatting = Formatting.Indented;

            _serializer.Serialize(jw, data);

            await jw.FlushAsync();
        }

        private async Task SaveBsonFile(object data, string fileName)
        {
            Directory.CreateDirectory(_path);

            var fullName = Path.Combine(_path, fileName);

            using var file = new FileStream(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var bson = new BsonDataWriter(file);

            _serializer.Serialize(bson, data);

            await bson.FlushAsync();
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
                AddedResources = data.AddedResources.Select(addedResource => new SerilizableParticipant(addedResource)).Cast<IParticipant>().ToList(),
                RemovedResources = data.RemovedResources.Select(removedResource => new SerilizableParticipant(removedResource)).Cast<IParticipant>().ToList()
            };

            await SaveJsonFile(participant, $"{DateTime.UtcNow.Ticks}-participant.json");
        }

        private async Task SaveRequests(string data)
        {
            Directory.CreateDirectory(_path);

            var name = DateTime.UtcNow.Ticks.ToString();
            var fullName = Path.Combine(_path, name);

            byte[] encodedText = Encoding.Unicode.GetBytes(data);

            using (FileStream sourceStream = new($"{fullName}.json", FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
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

        public async Task Finalise()
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
