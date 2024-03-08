namespace RecordingBot.Services.Contract
{
    public interface IEventPublisher
    {
        void Publish(string Subject, string Message, string TopicName = "");
    }
}
