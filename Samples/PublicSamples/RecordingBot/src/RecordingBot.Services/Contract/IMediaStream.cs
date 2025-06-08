using Microsoft.Graph.Communications.Calls;
using Microsoft.Skype.Bots.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecordingBot.Services.Contract
{
    public interface IMediaStream
    {
        Task AppendAudioBuffer(AudioMediaBuffer buffer, List<IParticipant> participant);
        Task End();
    }
}
