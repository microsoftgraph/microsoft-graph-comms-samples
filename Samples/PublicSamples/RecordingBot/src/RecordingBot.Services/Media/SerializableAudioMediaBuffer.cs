using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RecordingBot.Services.Media
{
    public class SerializableAudioMediaBuffer : IDisposable
    {
        private readonly List<IParticipant> _participants;

        public uint[] ActiveSpeakers { get; set; }
        public long Length { get; set; }
        public bool IsSilence { get; set; }
        public long Timestamp { get; set; }
        public byte[] Buffer { get; set; }
        public SerializableUnmixedAudioBuffer[] SerializableUnmixedAudioBuffers { get; set; }

        public SerializableAudioMediaBuffer()
        { }

        public SerializableAudioMediaBuffer(AudioMediaBuffer buffer, List<IParticipant> participants)
        {
            _participants = participants;

            Length = buffer.Length;
            ActiveSpeakers = buffer.ActiveSpeakers;
            IsSilence = buffer.IsSilence;
            Timestamp = buffer.Timestamp;

            if (Length > 0)
            {
                Buffer = new byte[Length];
                Marshal.Copy(buffer.Data, Buffer, 0, (int)Length);
            }

            if (buffer.UnmixedAudioBuffers != null)
            {
                SerializableUnmixedAudioBuffers = buffer.UnmixedAudioBuffers
                    .Where(unmixedBuffer => unmixedBuffer.Length > 0)
                    .Select(unmixedBuffer => new SerializableUnmixedAudioBuffer(unmixedBuffer, GetParticipantFromMSI(unmixedBuffer.ActiveSpeakerId)))
                    .ToArray();
            }
        }

        private IParticipant GetParticipantFromMSI(uint msi)
        {
            return _participants.SingleOrDefault(
                participant => participant.Resource.IsInLobby == false
                            && participant.Resource.MediaStreams
                                .Any(mediaStreamy => mediaStreamy.SourceId == msi.ToString()));
        }

        public void Dispose()
        {
            SerializableUnmixedAudioBuffers = null;
            Buffer = null;

            GC.SuppressFinalize(this);
        }

        public class SerializableUnmixedAudioBuffer
        {
            public uint ActiveSpeakerId { get; set; }
            public long Length { get; set; }
            public long OriginalSenderTimestamp { get; set; }
            public string DisplayName { get; set; }
            public string AdId { get; set; }
            public IDictionary<string, object> AdditionalData { get; set; }
            public byte[] Buffer { get; set; }

            public SerializableUnmixedAudioBuffer()
            { }

            public SerializableUnmixedAudioBuffer(UnmixedAudioBuffer buffer, IParticipant participant)
            {
                ActiveSpeakerId = buffer.ActiveSpeakerId;
                Length = buffer.Length;
                OriginalSenderTimestamp = buffer.OriginalSenderTimestamp;

                var identity = AddParticipant(participant);

                if (identity != null)
                {
                    DisplayName = identity.DisplayName;
                    AdId = identity.Id;
                }
                else
                {
                    var user = participant?.Resource?.Info?.Identity?.User;
                    if (user != null)
                    {
                        DisplayName = user.DisplayName;
                        AdId = user.Id;
                        AdditionalData = user.AdditionalData;
                    }
                }

                Buffer = new byte[Length];
                Marshal.Copy(buffer.Data, Buffer, 0, (int)Length);
            }

            private static Identity AddParticipant(IParticipant participant)
            {
                if (participant?.Resource?.Info?.Identity?.AdditionalData != null)
                {
                    foreach (var identity in participant.Resource.Info.Identity.AdditionalData)
                    {
                        if (identity.Key != "applicationInstance" && identity.Value is Identity)
                        {
                            return identity.Value as Identity;
                        }
                    }
                }

                return null;
            }
        }
    }
}
