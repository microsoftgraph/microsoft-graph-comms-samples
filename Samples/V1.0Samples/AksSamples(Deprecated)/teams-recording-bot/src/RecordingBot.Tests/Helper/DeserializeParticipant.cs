// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="DeserializeParticipant.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using RecordingBot.Model.Extension;
using RecordingBot.Services.Media;
using System.Collections.Generic;

namespace RecordingBot.Tests.Helper
{
    /// <summary>
    /// Class DeserializeParticipant.
    /// </summary>
    public class DeserializeParticipant
    {
        /// <summary>
        /// Gets the participant.
        /// </summary>
        /// <param name="serialized">The serialized.</param>
        /// <returns>List&lt;IParticipant&gt;.</returns>
        public List<IParticipant> GetParticipant(SerializableAudioMediaBuffer serialized)
        {
            var list = new List<IParticipant>();

            if (serialized.SerializableUnmixedAudioBuffers != null)
            {
                foreach (var i in serialized.SerializableUnmixedAudioBuffers)
                {
                    var participant = new Participant();
                    var info = new ParticipantInfo();
                    var identity = new IdentitySet();
                    var user = new Identity();

                    user.DisplayName = i.DisplayName;
                    user.AdditionalData = i.AdditionalData;
                    user.Id = i.AdId;

                    identity.User = user;
                    info.Identity = identity;
                    participant.Info = info;

                    var media = new Microsoft.Graph.MediaStream() { SourceId = i.ActiveSpeakerId.ToString() };
                    participant.MediaStreams = new List<Microsoft.Graph.MediaStream>() { media };

                    participant.IsInLobby = false;

                    list.Add(new ParticipantExtension(participant));
                }
            }

            return list;
        }
    }
}
