// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="DeserializeAudioMediaBuffer.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Skype.Bots.Media;
using RecordingBot.Services.Media;
using System;
using System.Runtime.InteropServices;

namespace RecordingBot.Tests.Helper
{
    /// <summary>
    /// Class DeserializeAudioMediaBuffer.
    /// Implements the <see cref="Microsoft.Skype.Bots.Media.AudioMediaBuffer" />
    /// </summary>
    /// <seealso cref="Microsoft.Skype.Bots.Media.AudioMediaBuffer" />
    public class DeserializeAudioMediaBuffer : AudioMediaBuffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializeAudioMediaBuffer" /> class.

        /// </summary>
        /// <param name="serialized">The serialized.</param>
        public DeserializeAudioMediaBuffer(SerializableAudioMediaBuffer serialized)
        {
            this.ActiveSpeakers = serialized.ActiveSpeakers;
            this.Length = serialized.Length;
            this.IsSilence = serialized.IsSilence;
            this.Timestamp = serialized.Timestamp;

            if (serialized.Buffer != null)
            {
                IntPtr mixedBuffer = Marshal.AllocHGlobal(serialized.Buffer.Length);
                Marshal.Copy(serialized.Buffer, 0, mixedBuffer, serialized.Buffer.Length);

                this.Data = mixedBuffer;
                this.Length = serialized.Buffer.Length;
            }

            if (serialized.SerializableUnmixedAudioBuffers != null)
            {
                this.UnmixedAudioBuffers = new UnmixedAudioBuffer[serialized.SerializableUnmixedAudioBuffers.Length];

                var count = 0;
                foreach (var i in serialized.SerializableUnmixedAudioBuffers)
                {
                    if (i.Buffer.Length > 0)
                    {
                        IntPtr bufferData = Marshal.AllocHGlobal(i.Buffer.Length);
                        Marshal.Copy(i.Buffer, 0, bufferData, i.Buffer.Length);

                        this.UnmixedAudioBuffers[count].Data(bufferData);

                        this.UnmixedAudioBuffers[count].ActiveSpeakerId(i.ActiveSpeakerId);
                        this.UnmixedAudioBuffers[count].OriginalSenderTimestamp(i.OriginalSenderTimestamp);
                        this.UnmixedAudioBuffers[count].Length(i.Buffer.Length);
                    }

                    count++;
                }
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <exception cref="NotImplementedException"></exception>
        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
