using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Models;
using RecordingBot.Model.Extension;
using RecordingBot.Services.Media;
using System.Collections.Generic;

namespace RecordingBot.Tests.Helper
{
    public class DeserializeParticipant
    {
        public static List<IParticipant> GetParticipant(SerializableAudioMediaBuffer serialized)
        {
            var list = new List<IParticipant>();

            if (serialized.SerializableUnmixedAudioBuffers != null)
            {
                foreach (var i in serialized.SerializableUnmixedAudioBuffers)
                {
                    var participant = new Participant();
                    var info = new ParticipantInfo();
                    var identity = new IdentitySet();
                    var user = new Identity
                    {
                        DisplayName = i.DisplayName,
                        AdditionalData = i.AdditionalData,
                        Id = i.AdId
                    };

                    identity.User = user;
                    info.Identity = identity;
                    participant.Info = info;

                    var media = new Microsoft.Graph.Models.MediaStream() { SourceId = i.ActiveSpeakerId.ToString() };
                    participant.MediaStreams = [media];

                    participant.IsInLobby = false;

                    list.Add(new ParticipantExtension(participant));
                }
            }

            return list;
        }
    }
}
