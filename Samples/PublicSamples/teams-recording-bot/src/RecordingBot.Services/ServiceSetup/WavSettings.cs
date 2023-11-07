// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : dannygar
// Created          : 09-08-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-08-2020
// ***********************************************************************
// <copyright file="AudioSettings.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace RecordingBot.Services.ServiceSetup
{
    /// <summary>
    /// Class WAVSettings.
    /// </summary>
    public class WAVSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WAVSettings"/> class.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        /// <param name="quality">The quality.</param>
        public WAVSettings(int sampleRate, int quality)
        {
            SampleRate = sampleRate;
            Quality = quality;
        }
        /// <summary>
        /// Gets or sets the sample rate.
        /// </summary>
        /// <value>The sample rate.</value>
        public int? SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public int? Quality { get; set; }
    }
}
