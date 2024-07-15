// <copyright file="ChildCallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot.Grouping
{
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Graph.Models;
    using Sample.Common.Beta.Logging;

#pragma warning disable SA1600 // Elements should be documented
    internal class ChildCallHandler : BaseReferenceCountingService
#pragma warning restore SA1600 // Elements should be documented
    {
        private readonly ICall parentCall;
        private readonly Call call;
        private readonly IConfiguration configuration;

#pragma warning disable SA1600 // Elements should be documented
        public ChildCallHandler(ICall parentCall, Call call, IConfiguration configuration)
    #pragma warning restore SA1600 // Elements should be documented
        {
            this.parentCall = parentCall;

            // parentCall.OnUpdated += this.CallOnUpdated;
            this.call = call;
            this.configuration = configuration;

            if (!configuration.SingleAudioStream)
            {
                var audioBufferAsyncWriter = new AudioBufferAsyncWriter(this.parentCall, configuration.DisableAudioStreamIo);

                // attach the botMediaStream
                this.BotMediaStream =
                    new BotMediaStream(this.parentCall.GetLocalMediaSession(), audioBufferAsyncWriter, parentCall?.GraphLogger, this.configuration.DisableAudioStreamIo);
            }
        }

        /// <summary>
        /// Gets the bot media stream.
        /// </summary>
        public BotMediaStream BotMediaStream { get; private set; }

#pragma warning disable SA1600 // Elements should be documented
        public void SubscribeForParticipantsCollectionUpdate()
    #pragma warning restore SA1600 // Elements should be documented
        {
            this.parentCall.Participants.OnUpdated += this.ParticipantsOnUpdated;
        }

#pragma warning disable SA1600
        public void SubscribeForParticipantUpdate(IParticipant participant)
#pragma warning restore SA1600
        {
            participant.OnUpdated += this.ParticipantOnUpdated;
        }

#pragma warning disable SA1600 // Elements should be documented
        public void UnSubscribeForParticipantsCollectionUpdate()
#pragma warning restore SA1600 // Elements should be documented
        {
            this.parentCall.Participants.OnUpdated -= this.ParticipantsOnUpdated;
        }

#pragma warning disable SA1600
        public void UnSubscribeForParticipantUpdate(IParticipant participant)
#pragma warning restore SA1600
        {
            participant.OnUpdated -= this.ParticipantOnUpdated;
        }

        /// <summary>
        /// Disposes the child call handler.
        /// </summary>
        public void Dispose()
        {
            if (this.DecrementReferenceCount())
            {
                this.BotMediaStream.Dispose();
            }
        }

        /// <summary>
        /// Call on updated event handler.
        /// </summary>
        /// <param name="sender">sender.</param>
        /// <param name="e">argument.</param>
        private void CallOnUpdated(ICall sender, ResourceEventArgs<Call> e)
        {
            RequestTelemetryHelper.OnNotificationReceived(e.AdditionalData);
        }

#pragma warning disable SA1600 // Elements should be documented
        private void ParticipantOnUpdated(IParticipant sender, ResourceEventArgs<Participant> e)
#pragma warning restore SA1600 // Elements should be documented
        {
            this.parentCall.GraphLogger.Warn($"{nameof(this.ParticipantOnUpdated)} inside child call {this.call.Id} for participant {e.ResourcePath}");
        }

#pragma warning disable SA1600 // Elements should be documented
        private void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> e)
    #pragma warning restore SA1600 // Elements should be documented
        {
            this.parentCall.GraphLogger.Warn($"{nameof(this.ParticipantsOnUpdated)} for Child call - {this.call.Id}");
        }
    }
}