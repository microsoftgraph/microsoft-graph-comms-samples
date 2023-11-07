// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="IEventGridPublisher.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace RecordingBot.Services.Contract
{
    /// <summary>
    /// Interface IEventPublisher
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes the specified subject.
        /// </summary>
        /// <param name="Subject">The subject.</param>
        /// <param name="Message">The message.</param>
        /// <param name="TopicName">Name of the topic.</param>
        void Publish(string Subject, string Message, string TopicName = "");
    }
}
