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
        private List<IParticipant> _participants;

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
                SerializableUnmixedAudioBuffers = new SerializableUnmixedAudioBuffer[buffer.UnmixedAudioBuffers.Length];
                for (var i = 0; i < buffer.UnmixedAudioBuffers.Length; i++)
                {
                    if (buffer.UnmixedAudioBuffers[i].Length > 0)
                    {
                        var speakerId = buffer.UnmixedAudioBuffers[i].ActiveSpeakerId;
                        var unmixedAudioBuffer = new SerializableUnmixedAudioBuffer(buffer.UnmixedAudioBuffers[i], _getParticipantFromMSI(speakerId));
                        SerializableUnmixedAudioBuffers[i] = unmixedAudioBuffer;
                    }
                }
            }
        }

        private IParticipant _getParticipantFromMSI(uint msi)
        {
            return _participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }

        public void Dispose()
        {
            SerializableUnmixedAudioBuffers = null;
            Buffer = null;
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

                var i = AddParticipant(participant);

                if (i != null)
                {
                    DisplayName = i.DisplayName;
                    AdId = i.Id;
                }
                else
                {
                    DisplayName = participant?.Resource?.Info?.Identity?.User?.DisplayName;
                    AdId = participant?.Resource?.Info?.Identity?.User?.Id;
                    AdditionalData = participant?.Resource?.Info?.Identity?.User?.AdditionalData;
                }

                Buffer = new byte[Length];
                Marshal.Copy(buffer.Data, Buffer, 0, (int)Length);
            }

            private Identity AddParticipant(IParticipant p)
            {
                if (p?.Resource?.Info?.Identity?.AdditionalData != null)
                {
                    foreach (var i in p.Resource.Info.Identity.AdditionalData)
                    {
                        if (i.Key != "applicationInstance" && i.Value is Identity)
                        {
                            return i.Value as Identity;
                        }
                    }
                }

                return null;
            }
        }
    }
}
