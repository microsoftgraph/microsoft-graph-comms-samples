// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="IMediaStream.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph.Communications.Calls;
using Microsoft.Skype.Bots.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecordingBot.Services.Contract
{
    /// <summary>
    /// Interface IMediaStream
    /// </summary>
    public interface IMediaStream
    {
        /// <summary>
        /// Appends the audio buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="participant">The participant.</param>
        /// <returns>Task.</returns>
        Task AppendAudioBuffer(AudioMediaBuffer buffer, List<IParticipant> participant);
        /// <summary>
        /// Ends this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task End();
    }
}
