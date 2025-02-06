namespace RecordingBot.Services.ServiceSetup
{
    public class WAVSettings
    {
        public int? SampleRate { get; set; }
        public int? Quality { get; set; }

        public WAVSettings(int sampleRate, int quality)
        {
            SampleRate = sampleRate;
            Quality = quality;
        }
    }
}
