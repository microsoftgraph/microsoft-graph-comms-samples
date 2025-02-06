using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using RecordingBot.Model.Models;
using RecordingBot.Services.Bot;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RecordingBot.Services.Contract
{
    public interface IBotService : Model.Contracts.IInitializable
    {
        ConcurrentDictionary<string, CallHandler> CallHandlers { get; }
        ICommunicationsClient Client { get; }
        Task EndCallByCallLegIdAsync(string callLegId);
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);
    }
}
