using Microsoft.Graph.Communications.Calls;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordingBot.Model.Models
{
    public class ParticipantData : IParsable
    {
        public ICollection<IParticipant> AddedResources { get; set; }
        public ICollection<IParticipant> RemovedResources { get; set; }

        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>()
            {
                {
                    "addedResources",
                    delegate(IParseNode n)
                    {
                        AddedResources = (ICollection<IParticipant>)n.GetCollectionOfObjectValues(ParticipantExtension.CreateFromDiscriminatorValue).ToList();
                    }
                },
                {
                    "removedResources",
                    delegate(IParseNode n)
                    {
                        RemovedResources = (ICollection<IParticipant>)n.GetCollectionOfObjectValues(ParticipantExtension.CreateFromDiscriminatorValue).ToList();
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

            writer.WriteCollectionOfObjectValues("addedResources", AddedResources.Cast<ParticipantExtension>());
            writer.WriteCollectionOfObjectValues("removedResources", RemovedResources.Cast<ParticipantExtension>());
        }

        public static ParticipantData CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            if (parseNode == null)
            {
                throw new ArgumentNullException("parseNode");
            }

            return new ParticipantData();
        }
    }
}
