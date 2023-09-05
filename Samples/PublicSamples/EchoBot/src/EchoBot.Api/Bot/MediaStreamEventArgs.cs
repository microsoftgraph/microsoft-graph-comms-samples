using Microsoft.Skype.Bots.Media;

namespace EchoBot.Api.Bot
{
    public class MediaStreamEventArgs
    {
        public List<AudioMediaBuffer> AudioMediaBuffers { get; set; }
    }
}
