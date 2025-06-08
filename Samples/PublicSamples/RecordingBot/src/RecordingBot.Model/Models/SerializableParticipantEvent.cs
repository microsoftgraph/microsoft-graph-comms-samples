using Microsoft.Graph.Communications.Calls;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordingBot.Model.Models
{
    public class SerializableParticipantEvent : IParsable
    {
        public List<IParticipant> AddedResources { get; set; }
        public List<IParticipant> RemovedResources { get; set; }

        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>()
            {
                {
                    "addedResources",
                    delegate(IParseNode n)
                    {
                        AddedResources = n.GetCollectionOfObjectValues(SerilizableParticipant.CreateFromDiscriminatorValue).Cast<IParticipant>().ToList();
                    }
                },
                {
                    "removedResources",
                    delegate(IParseNode n)
                    {
                        RemovedResources = n.GetCollectionOfObjectValues(SerilizableParticipant.CreateFromDiscriminatorValue).Cast<IParticipant>().ToList();
                    }
                }
            };
        }

        public void Serialize(ISerializationWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteCollectionOfObjectValues("addedResources", AddedResources.Cast<SerilizableParticipant>());
            writer.WriteCollectionOfObjectValues("removedResources", RemovedResources.Cast<SerilizableParticipant>());
        }

        public static SerializableParticipantEvent CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            ArgumentNullException.ThrowIfNull(parseNode);

            return new SerializableParticipantEvent();
        }
    }
}
