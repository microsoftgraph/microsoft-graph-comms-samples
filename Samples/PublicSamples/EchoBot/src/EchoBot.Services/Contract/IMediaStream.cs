// ***********************************************************************
// Assembly         : EchoBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="IMediaStream.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using EchoBot.Services.Media;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EchoBot.Services.Contract
{
    /// <summary>
    /// Interface IMediaStream
    /// </summary>
    public interface IMediaStream
    {
        event EventHandler<MediaStreamEventArgs> SendMediaBuffer;

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
