using Microsoft.Skype.Bots.Media;
using RecordingBot.Services.Media;
using System;
using System.Runtime.InteropServices;

namespace RecordingBot.Tests.Helper
{
    public class DeserializeAudioMediaBuffer : AudioMediaBuffer
    {
        public DeserializeAudioMediaBuffer(SerializableAudioMediaBuffer serialized)
        {
            ActiveSpeakers = serialized.ActiveSpeakers;
            Length = serialized.Length;
            IsSilence = serialized.IsSilence;
            Timestamp = serialized.Timestamp;

            if (serialized.Buffer != null)
            {
                IntPtr mixedBuffer = Marshal.AllocHGlobal(serialized.Buffer.Length);
                Marshal.Copy(serialized.Buffer, 0, mixedBuffer, serialized.Buffer.Length);

                Data = mixedBuffer;
                Length = serialized.Buffer.Length;
            }

            if (serialized.SerializableUnmixedAudioBuffers != null)
            {
                UnmixedAudioBuffers = new UnmixedAudioBuffer[serialized.SerializableUnmixedAudioBuffers.Length];

                var count = 0;
                foreach (var i in serialized.SerializableUnmixedAudioBuffers)
                {
                    if (i.Buffer.Length > 0)
                    {
                        IntPtr bufferData = Marshal.AllocHGlobal(i.Buffer.Length);
                        Marshal.Copy(i.Buffer, 0, bufferData, i.Buffer.Length);

                        UnmixedAudioBuffers[count].Data(bufferData);

                        UnmixedAudioBuffers[count].ActiveSpeakerId(i.ActiveSpeakerId);
                        UnmixedAudioBuffers[count].OriginalSenderTimestamp(i.OriginalSenderTimestamp);
                        UnmixedAudioBuffers[count].Length(i.Buffer.Length);
                    }

                    count++;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
