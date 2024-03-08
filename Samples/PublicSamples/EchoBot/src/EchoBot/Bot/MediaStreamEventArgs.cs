using Microsoft.Skype.Bots.Media;

namespace EchoBot.Bot
{
    public class MediaStreamEventArgs
    {
        public List<AudioMediaBuffer> AudioMediaBuffers { get; set; }
    }
}
