using Microsoft.Skype.Bots.Media;

namespace RecordingBot.Services.Media
{
    public class SerializableAudioQualityOfExperienceData
    {
        public string Id;
        public long AverageInBoundNetworkJitter;
        public long MaximumInBoundNetworkJitter;
        public long TotalMediaDuration;

        public SerializableAudioQualityOfExperienceData(string Id, AudioQualityOfExperienceData aQoE)
        {
            this.Id = Id;
            AverageInBoundNetworkJitter = aQoE.AudioMetrics.AverageInboundNetworkJitter.Ticks;
            MaximumInBoundNetworkJitter = aQoE.AudioMetrics.MaximumInboundNetworkJitter.Ticks;
            TotalMediaDuration = aQoE.TotalMediaDuration.Ticks;
        }
    }
}
