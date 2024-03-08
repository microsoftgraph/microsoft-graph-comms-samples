using Microsoft.Graph.Communications.Calls;
using System.Collections.Generic;

namespace RecordingBot.Model.Models
{
    public class ParticipantData
    {
        public ICollection<IParticipant> AddedResources { get; set; }
        public ICollection<IParticipant> RemovedResources { get; set; }
    }
}
