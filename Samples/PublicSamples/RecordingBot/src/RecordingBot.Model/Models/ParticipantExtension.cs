using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecordingBot.Model.Models
{
    public class ParticipantExtension : IParticipant, IParsable
    {
        public Participant Resource { get; set; }

        public DateTimeOffset ModifiedDateTime { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public string ResourcePath { get; set; }

        public string Id { get; set; }
        [JsonIgnore]
        object IResource.Resource { get; }
        [JsonIgnore]
        public ICommunicationsClient Client { get; set; }
        [JsonIgnore]
        public IGraphClient GraphClient { get; set; }
        [JsonIgnore]
        public IGraphLogger GraphLogger { get; set; }

        [JsonConstructor]
        public ParticipantExtension(IParticipant participant)
        {
            if (participant != null)
            {
                Resource = participant.Resource;
                ResourcePath = participant.ResourcePath;
                ModifiedDateTime = participant.ModifiedDateTime;
                CreatedDateTime = participant.CreatedDateTime;
            }
        }

        public ParticipantExtension(Participant participant)
        {
            Resource = participant;
        }

        public ParticipantExtension()
        { }

        public event ResourceEventHandler<IParticipant, Participant> OnUpdated;

        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>()
            {
                {
                    "resource",
                    delegate(IParseNode n)
                    {
                        Resource = n.GetObjectValue(Participant.CreateFromDiscriminatorValue);
                    }
                },
                {
                    "modifiedDateTime",
                    delegate(IParseNode n)
                    {
                        ModifiedDateTime = n.GetDateTimeOffsetValue() ?? default;
                    }
                },
                {
                    "createdDateTime",
                    delegate(IParseNode n)
                    {
                        CreatedDateTime = n.GetDateTimeOffsetValue() ?? default;
                    }
                },
                {
                    "resourcePath",
                        delegate(IParseNode n)
                        {
                        ResourcePath = n.GetStringValue();
                    }
                }
            };
        }

        public void Serialize(ISerializationWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteObjectValue("resource", Resource);
            writer.WriteDateTimeOffsetValue("modifiedDateTime", ModifiedDateTime);
            writer.WriteDateTimeOffsetValue("createdDateTime", CreatedDateTime);
            writer.WriteStringValue("ResourcePath", ResourcePath);
        }

        public static ParticipantExtension CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            if (parseNode == null)
            {
                throw new ArgumentNullException("parseNode");
            }

            return new ParticipantExtension();
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

        public Task DeleteAsync(bool handleHttpNotFoundInternally = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
