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
    public class SerilizableParticipant : IParticipant, IParsable
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
        public SerilizableParticipant(IParticipant participant)
        {
            if (participant != null)
            {
                Resource = participant.Resource;
                ResourcePath = participant.ResourcePath;
                ModifiedDateTime = participant.ModifiedDateTime;
                CreatedDateTime = participant.CreatedDateTime;
            }
        }

        public SerilizableParticipant(Participant participant)
        {
            Resource = participant;
        }

        public SerilizableParticipant()
        { }

#pragma warning disable CS0067 // The event 'SerilizableParticipant.OnUpdated' is never used
        public event ResourceEventHandler<IParticipant, Participant> OnUpdated;
#pragma warning restore CS0067 // The event 'SerilizableParticipant.OnUpdated' is never used

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
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteObjectValue("resource", Resource);
            writer.WriteDateTimeOffsetValue("modifiedDateTime", ModifiedDateTime);
            writer.WriteDateTimeOffsetValue("createdDateTime", CreatedDateTime);
            writer.WriteStringValue("ResourcePath", ResourcePath);
        }

        public static SerilizableParticipant CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            ArgumentNullException.ThrowIfNull(parseNode);

            return new SerilizableParticipant();
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

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            throw new NotImplementedException();
        }
    }
}
