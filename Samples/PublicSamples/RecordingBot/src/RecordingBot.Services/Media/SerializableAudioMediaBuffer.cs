// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="SerializableAudioMediaBuffer.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RecordingBot.Services.Media
{
    /// <summary>
    /// Class SerializableAudioMediaBuffer.
    /// Implements the <see cref="System.IDisposable" />
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class SerializableAudioMediaBuffer : IDisposable
    {
        /// <summary>
        /// Gets or sets the active speakers.
        /// </summary>
        /// <value>The active speakers.</value>
        public uint[] ActiveSpeakers { get; set; }
        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public long Length { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is silence.
        /// </summary>
        /// <value><c>true</c> if this instance is silence; otherwise, <c>false</c>.</value>
        public bool IsSilence { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public long Timestamp { get; set; }
        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>The buffer.</value>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the serializable unmixed audio buffers.
        /// </summary>
        /// <value>The serializable unmixed audio buffers.</value>
        public SerializableUnmixedAudioBuffer[] SerializableUnmixedAudioBuffers { get; set; }

        /// <summary>
        /// The participants
        /// </summary>
        private List<IParticipant> participants;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableAudioMediaBuffer" /> class.

        /// </summary>
        public SerializableAudioMediaBuffer()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableAudioMediaBuffer" /> class.

        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="participants">The participants.</param>
        public SerializableAudioMediaBuffer(AudioMediaBuffer buffer, List<IParticipant> participants)
        {
            this.participants = participants;

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

        /// <summary>
        /// Gets the participant from msi.
        /// </summary>
        /// <param name="msi">The msi.</param>
        /// <returns>IParticipant.</returns>
        private IParticipant _getParticipantFromMSI(uint msi)
        {
            return this.participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            SerializableUnmixedAudioBuffers = null;
            Buffer = null;
        }

        /// <summary>
        /// Class SerializableUnmixedAudioBuffer.
        /// </summary>
        public class SerializableUnmixedAudioBuffer
        {
            /// <summary>
            /// Gets or sets the active speaker identifier.
            /// </summary>
            /// <value>The active speaker identifier.</value>
            public uint ActiveSpeakerId { get; set; }
            /// <summary>
            /// Gets or sets the length.
            /// </summary>
            /// <value>The length.</value>
            public long Length { get; set; }
            /// <summary>
            /// Gets or sets the original sender timestamp.
            /// </summary>
            /// <value>The original sender timestamp.</value>
            public long OriginalSenderTimestamp { get; set; }
            /// <summary>
            /// Gets or sets the display name.
            /// </summary>
            /// <value>The display name.</value>
            public string DisplayName { get; set; }
            /// <summary>
            /// Gets or sets the ad identifier.
            /// </summary>
            /// <value>The ad identifier.</value>
            public string AdId { get; set; }
            /// <summary>
            /// Gets or sets the additional data.
            /// </summary>
            /// <value>The additional data.</value>
            public IDictionary<string, object> AdditionalData { get; set; }

            /// <summary>
            /// Gets or sets the buffer.
            /// </summary>
            /// <value>The buffer.</value>
            public byte[] Buffer { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SerializableUnmixedAudioBuffer" /> class.

            /// </summary>
            public SerializableUnmixedAudioBuffer()
            {

            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SerializableUnmixedAudioBuffer" /> class.

            /// </summary>
            /// <param name="buffer">The buffer.</param>
            /// <param name="participant">The participant.</param>
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

            /// <summary>
            /// Adds the participant.
            /// </summary>
            /// <param name="p">The p.</param>
            /// <returns>Identity.</returns>
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
