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
            CompressionCode = 1;            // PCM
            NumberOfChannels = 1;           // No Stereo
            SampleRate = 16000;             // 16khz only
            AvgBytesPerSecond = 32000;
        }

        public short CompressionCode { get; set; }
        public short NumberOfChannels { get; set; }
        public int SampleRate { get; set; }
        public int AvgBytesPerSecond { get; set; }
    }
}
