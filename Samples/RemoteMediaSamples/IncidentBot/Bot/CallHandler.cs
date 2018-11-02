// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Bot
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Core.Serialization;
    using Microsoft.Graph.Communications.Resources;

    /// <summary>
    /// Base class for call handler for event handling, logging and cleanup.
    /// </summary>
    public class CallHandler : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="call">The call.</param>
        public CallHandler(Bot bot, ICall call)
        {
            this.Bot = bot;
            this.Call = call;

            // Use the call GraphLogger so we carry the call/correlation context in each log record.
            this.Logger = call.GraphLogger.CreateShim(component: this.GetType().Name);

            var outcome = Serializer.SerializeObject(call.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Created:\n" + outcome);

            this.Call.OnUpdated += this.OnCallUpdated;
            this.Call.Participants.OnUpdated += this.OnParticipantsUpdated;
        }

        /// <summary>
        /// Gets the call interface.
        /// </summary>
        public ICall Call { get; }

        /// <summary>
        /// Gets the outcomes log - maintained for easy checking of async server responses.
        /// </summary>
        /// <value>
        /// The outcomes log.
        /// </value>
        public LinkedList<string> OutcomesLogMostRecentFirst { get; } = new LinkedList<string>();

        /// <summary>
        /// Gets the bot.
        /// </summary>
        protected Bot Bot { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected IGraphLogger Logger { get; }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        private static Serializer Serializer { get; } = new Serializer(pretty: true);

        /// <inheritdoc />
        public void Dispose()
        {
            this.Call.OnUpdated -= this.OnCallUpdated;
            this.Call.Participants.OnUpdated -= this.OnParticipantsUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }
        }

        /// <summary>
        /// The event handler when call is updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        protected virtual void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// The event handler when participants are updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        protected virtual void ParticipantsOnUpdated(ICallParticipantCollection sender, CollectionEventArgs<ICallParticipant> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler when participan is updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        protected virtual void ParticipantOnUpdated(ICallParticipant sender, ResourceEventArgs<Participant> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler for call updated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments.</param>
        private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Updated:\n" + outcome);

            this.CallOnUpdated(sender, args);
        }

        /// <summary>
        /// Event handler when participan is updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void OnParticipantUpdated(ICallParticipant sender, ResourceEventArgs<Participant> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Participant Updated:\n" + outcome);

            this.ParticipantOnUpdated(sender, args);
        }

        /// <summary>
        /// The event handler when participants are updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void OnParticipantsUpdated(ICallParticipantCollection sender, CollectionEventArgs<ICallParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Added:\n" + outcome);

                participant.OnUpdated += this.OnParticipantUpdated;
            }

            foreach (var participant in args.RemovedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Removed:\n" + outcome);

                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.ParticipantsOnUpdated(sender, args);
        }
    }
}
