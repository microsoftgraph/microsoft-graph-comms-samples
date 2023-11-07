// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="RecordingBotSettings.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace RecordingBot.Model.Settings
{
    /// <summary>
    /// Class RecordingBotSettings.
    /// </summary>
    public class RecordingBotSettings
    {
        /// <summary>
        /// Gets or sets the service point manager default connection limit.
        /// </summary>
        /// <value>The service point manager default connection limit.</value>
        public int ServicePointManagerDefaultConnectionLimit { get; set; } = 12;
    }
}
