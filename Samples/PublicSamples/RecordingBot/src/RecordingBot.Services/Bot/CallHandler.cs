using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace RecordingBot.Services.Bot
{
    public class CallHandler : HeartbeatHandler
    {
        public ICall Call { get; }
        public BotMediaStream BotMediaStream { get; private set; }
        private int recordingStatusIndex = -1;
        private readonly AzureSettings _settings;
        private readonly IEventPublisher _eventPublisher;
        private CaptureEvents _capture;
        private bool _isDisposed = false;

        public CallHandler(ICall statefulCall, IAzureSettings settings, IEventPublisher eventPublisher) : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger)
        {
            _settings = (AzureSettings)settings;
            _eventPublisher = eventPublisher;

            Call = statefulCall;
            Call.OnUpdated += CallOnUpdated;
            Call.Participants.OnUpdated += ParticipantsOnUpdated;

            BotMediaStream = new BotMediaStream(Call.GetLocalMediaSession(), Call.Id, GraphLogger, eventPublisher, _settings);

            if (_settings.CaptureEvents)
            {
                var path = Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, _settings.EventsFolder, statefulCall.GetLocalMediaSession().MediaSessionId.ToString(), "participants");
                _capture = new CaptureEvents(path);
            }
        }

        /// <inheritdoc/>
        protected override Task HeartbeatAsync(ElapsedEventArgs args)
        {
            return Call.KeepAliveAsync();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _isDisposed = true;
            Call.OnUpdated -= CallOnUpdated;
            Call.Participants.OnUpdated -= ParticipantsOnUpdated;

            BotMediaStream?.Dispose();

            // Event - Dispose of the call completed ok
            _eventPublisher.Publish("CallDisposedOK", $"Call.Id: {Call.Id}");
        }

        private void OnRecordingStatusFlip(ICall source)
        {
            _ = Task.Run(async () =>
            {
                // TODO: consider rewriting the recording status checking
                var recordingStatus = new[] { RecordingStatus.Recording, RecordingStatus.NotRecording, RecordingStatus.Failed };

                var recordingIndex = recordingStatusIndex + 1;
                if (recordingIndex >= recordingStatus.Length)
                {
                    var recordedParticipantId = Call.Resource.IncomingContext.ObservedParticipantId;

                    var recordedParticipant = Call.Participants[recordedParticipantId];
                    await recordedParticipant.DeleteAsync().ConfigureAwait(false);
                    // Event - Recording has ended
                    _eventPublisher.Publish("CallRecordingFlip", $"Call.Id: {Call.Id} ended");
                    return;
                }

                var newStatus = recordingStatus[recordingIndex];
                try
                {
                    // Event - Log the recording status
                    var status = Enum.GetName(typeof(RecordingStatus), newStatus);
                    _eventPublisher.Publish("CallRecordingFlip", $"Call.Id: {Call.Id} status changed to {status}");

                    // NOTE: if your implementation supports stopping the recording during the call, you can call the same method above with RecordingStatus.NotRecording
                    await source
                        .UpdateRecordingStatusAsync(newStatus)
                        .ConfigureAwait(false);

                    recordingStatusIndex = recordingIndex;
                }
                catch (Exception exc)
                {
                    // e.g. bot joins via direct join - may not have the permissions
                    GraphLogger.Error(exc, $"Failed to flip the recording status to {newStatus}");
                    // Event - Recording status exception - failed to update 
                    _eventPublisher.Publish("CallRecordingFlip", $"Failed to flip the recording status to {newStatus}");
                }
            }).ForgetAndLogExceptionAsync(GraphLogger);
        }

        private async void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
        {
            GraphLogger.Info($"Call status updated to {e.NewResource.State} - {e.NewResource.ResultInfo?.Message}");
            // Event - Recording update e.g established/updated/start/ended
            _eventPublisher.Publish($"Call{e.NewResource.State}", $"Call.ID {Call.Id} Sender.Id {sender.Id} status updated to {e.NewResource.State} - {e.NewResource.ResultInfo?.Message}");

            if (e.OldResource.State != e.NewResource.State && e.NewResource.State == CallState.Established)
            {
                if (!_isDisposed)
                {
                    // Call is established. We should start receiving Audio, we can inform clients that we have started recording.
                    OnRecordingStatusFlip(sender);
                }
            }

            if ((e.OldResource.State == CallState.Established) && (e.NewResource.State == CallState.Terminated))
            {
                if (BotMediaStream != null)
                {
                    var aQoE = BotMediaStream.GetAudioQualityOfExperienceData();

                    if (aQoE != null && _settings.CaptureEvents)
                    {
                        await _capture?.Append(aQoE);
                    }
                    await BotMediaStream.StopMedia();
                }

                if (_settings.CaptureEvents)
                    await _capture?.Finalise();
            }
        }

        private string createParticipantUpdateJson(string participantId, string participantDisplayName = "")
        {
            if (participantDisplayName.Length == 0)
                return "{" + string.Format($"\"Id\": \"{participantId}\"") + "}";
            else
                return "{" + string.Format($"\"Id\": \"{participantId}\", \"DisplayName\": \"{participantDisplayName}\"") + "}";
        }

        private string updateParticipant(List<IParticipant> participants, IParticipant participant, bool added, string participantDisplayName = "")
        {
            if (added)
                participants.Add(participant);
            else
                participants.Remove(participant);
            return createParticipantUpdateJson(participant.Id, participantDisplayName);
        }

        private void updateParticipants(ICollection<IParticipant> eventArgs, bool added = true)
        {
            foreach (var participant in eventArgs)
            {
                var json = string.Empty;

                // todo remove the cast with the new graph implementation,
                // for now we want the bot to only subscribe to "real" participants
                var participantDetails = participant.Resource.Info.Identity.User;

                if (participantDetails != null)
                {
                    json = updateParticipant(this.BotMediaStream.participants, participant, added, participantDetails.DisplayName);
                }
                else if (participant.Resource.Info.Identity.AdditionalData?.Count > 0)
                {
                    if (CheckParticipantIsUsable(participant))
                    {
                        json = updateParticipant(this.BotMediaStream.participants, participant, added);
                    }
                }

                if (json.Length > 0)
                    if (added)
                        _eventPublisher.Publish("CallParticipantAdded", json);
                    else
                        _eventPublisher.Publish("CallParticipantRemoved", json);
            }
        }

        public void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            if (_settings.CaptureEvents)
            {
                _capture?.Append(args);
            }
            updateParticipants(args.AddedResources);
            updateParticipants(args.RemovedResources, false);
        }

        private bool CheckParticipantIsUsable(IParticipant p)
        {
            foreach (var i in p.Resource.Info.Identity.AdditionalData)
                if (i.Key != "applicationInstance" && i.Value is Identity)
                    return true;

            return false;
        }
    }
}
