using Microsoft.Skype.Bots.Media;

namespace EchoBot.Api.Media
{
    public class MediaStreamEventArgs
    {
        public List<AudioMediaBuffer> AudioMediaBuffers { get; set; }
    }
}
