// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : dannygar
// Created          : 09-08-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-08-2020
// ***********************************************************************
// <copyright file="AudioConstants.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace RecordingBot.Model.Constants
{
    /// <summary>
    /// Class AudioConstants.
    /// </summary>
    public static class AudioConstants
    {
        /// <summary>
        /// The default sample rate
        /// </summary>
        public const int DefaultSampleRate = 16000;

        /// <summary>
        /// The default bits
        /// </summary>
        public const int DefaultBits = 16;

        /// <summary>
        /// The default channels
        /// </summary>
        public const int DefaultChannels = 1;

        /// <summary>
        /// The highest sampling quality level
        /// </summary>
        public const int HighestSamplingQualityLevel = 60;
    }
}
