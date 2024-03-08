using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RecordingBot.Model.Extension
{
    public class ParticipantExtension : IParticipant
    {
        public Participant Resource { get; set; }

        [JsonConstructor]
        public ParticipantExtension(IParticipant participant)
        {
            Resource = participant?.Resource;
            Id = participant?.Id;
            ResourcePath = participant?.ResourcePath;

            if (participant != null)
            {
                ModifiedDateTime = participant.ModifiedDateTime;
                participant.OnUpdated += OnUpdated;
            }
        }

        public ParticipantExtension(Participant participant)
        {
            Resource = participant;
        }

        public string Id { get; private set; }

        public DateTimeOffset ModifiedDateTime { get; private set; }

        public DateTimeOffset CreatedDateTime { get; private set; }

        [JsonIgnore]
        public ICommunicationsClient Client { get; private set; }

        [JsonIgnore]
        public IGraphClient GraphClient { get; private set; }

        [JsonIgnore]
        public IGraphLogger GraphLogger { get; private set; }

        public string ResourcePath { get; private set; }

        [JsonIgnore]
        object IResource.Resource => throw new NotImplementedException();

        public event ResourceEventHandler<IParticipant, Participant> OnUpdated;

        public Task DeleteAsync(bool handleHttpNotFoundInternally = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task MuteAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StartHoldMusicAsync(Prompt customPrompt, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopHoldMusicAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
