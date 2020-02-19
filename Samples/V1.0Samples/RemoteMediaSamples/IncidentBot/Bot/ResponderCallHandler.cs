// <copyright file="ResponderCallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Sample.IncidentBot.Data;
    using Sample.IncidentBot.IncidentStatus;

    /// <summary>
    /// The responder call handler class.
    /// </summary>
    public class ResponderCallHandler : CallHandler
    {
        private string responderId;

        private IncidentStatusData statusData;

        private int promptTimes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponderCallHandler"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="call">The call.</param>
        /// <param name="responderId">The responder id.</param>
        /// <param name="statusData">The incident status data.</param>
        public ResponderCallHandler(Bot bot, ICall call, string responderId, IncidentStatusData statusData)
            : base(bot, call)
        {
            this.responderId = responderId;
            this.statusData = statusData;

            this.statusData?.UpdateResponderNotificationCallId(this.responderId, call.Id, call.ScenarioId);
        }

        /// <inheritdoc/>
        protected override void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            this.statusData?.UpdateResponderNotificationStatus(this.responderId, sender.Resource.State);

            if (sender.Resource.State == CallState.Established)
            {
                var currentPromptTimes = Interlocked.Increment(ref this.promptTimes);

                if (currentPromptTimes == 1)
                {
                    this.SubscribeToTone();
                    this.PlayNotificationPrompt();
                }

                if (sender.Resource.ToneInfo?.Tone != null)
                {
                    Tone tone = sender.Resource.ToneInfo.Tone.Value;

                    this.GraphLogger.Info($"Tone {tone} received.");

                    // handle different tones from responder
                    switch (tone)
                    {
                        case Tone.Tone1:
                            this.PlayTransferingPrompt();
                            this.TransferToIncidentMeeting();
                            break;
                        case Tone.Tone0:
                        default:
                            this.PlayNotificationPrompt();
                            break;
                    }

                    sender.Resource.ToneInfo.Tone = null;
                }
            }
        }

        /// <summary>
        /// Subscribe to tone.
        /// </summary>
        private void SubscribeToTone()
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Call.SubscribeToToneAsync().ConfigureAwait(false);
                    this.GraphLogger.Info("Started subscribing to tone.");
                }
                catch (Exception ex)
                {
                    this.GraphLogger.Error(ex, $"Failed to subscribe to tone. ");
                    throw;
                }
            });
        }

        /// <summary>
        /// Play the transfering prompt.
        /// </summary>
        private void PlayTransferingPrompt()
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Call.PlayPromptAsync(new List<MediaPrompt> { this.Bot.MediaMap[Bot.TransferingPromptName] }).ConfigureAwait(false);
                    this.GraphLogger.Info("Started playing transfering prompt");
                }
                catch (Exception ex)
                {
                    this.GraphLogger.Error(ex, $"Failed to play transfering prompt.");
                    throw;
                }
            });
        }

        /// <summary>
        /// Play the notification prompt.
        /// </summary>
        private void PlayNotificationPrompt()
        {
            Task.Run(async () =>
            {
                try
                {
                    await this.Call.PlayPromptAsync(new List<MediaPrompt> { this.Bot.MediaMap[Bot.NotificationPromptName] }).ConfigureAwait(false);
                    this.GraphLogger.Info("Started playing notification prompt");
                }
                catch (Exception ex)
                {
                    this.GraphLogger.Error(ex, $"Failed to play notification prompt.");
                    throw;
                }
            });
        }

        /// <summary>
        /// add current responder to incident meeting as participant.
        /// </summary>
        private void TransferToIncidentMeeting()
        {
            Task.Run(async () =>
            {
                try
                {
                    var incidentMeetingCallId = this.statusData?.BotMeetingCallId;
                    var responderStatusData = this.statusData?.GetResponder(this.responderId);

                    if (incidentMeetingCallId != null && responderStatusData != null)
                    {
                        var addParticipantRequestData = new AddParticipantRequestData()
                        {
                            ObjectId = responderStatusData.ObjectId,
                            ReplacesCallId = responderStatusData.NotificationCallId,
                        };

                        await this.Bot.AddParticipantAsync(incidentMeetingCallId, addParticipantRequestData).ConfigureAwait(false);

                        this.GraphLogger.Info("Finished to transfer to incident meeting. ");
                    }
                    else
                    {
                        this.GraphLogger.Warn(
                            $"Tried to transfer to incident meeting but needed info are not valid. Meeting call-id: {incidentMeetingCallId}; status data: {responderStatusData}");
                    }
                }
                catch (Exception ex)
                {
                    this.GraphLogger.Error(ex, $"Failed to transfer to incident meeting.");
                    throw;
                }
            });
        }
    }
}
