// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="WavFileSettings.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace RecordingBot.Model.Settings
{
    /// <summary>
    /// wav file writer, this class will create a wav file
    /// from the received buffers in the smart agents.
    /// </summary>
    public class WavFileSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WavFileSettings" /> class.
        /// Default constructor with default PCM 16 mono.
        /// This class was taken and adapted from <see cref="https://github.com/Microsoft/BotBuilder-RealTimeMediaCalling/issues/19#issuecomment-311433357" />
        /// </summary>
        public WavFileSettings()
        {
            this.CompressionCode = 1;            // PCM
            this.NumberOfChannels = 1;           // No Stereo
            this.SampleRate = 16000;             // 16khz only
            this.AvgBytesPerSecond = 32000;
        }

        /// <summary>
        /// Gets or sets CompressionCode.
        /// </summary>
        /// <value>The compression code.</value>
        public short CompressionCode { get; set; }

        /// <summary>
        /// Gets or sets NumberOfChannels.
        /// </summary>
        /// <value>The number of channels.</value>
        public short NumberOfChannels { get; set; }

        /// <summary>
        /// Gets or sets SampleRate.
        /// </summary>
        /// <value>The sample rate.</value>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets or sets AvgBytesPerSecond.
        /// </summary>
        /// <value>The average bytes per second.</value>
        public int AvgBytesPerSecond { get; set; }
    }
}
