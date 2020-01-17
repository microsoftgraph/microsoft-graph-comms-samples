﻿// <copyright file="IncomingCallHandler.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// The call handler for incoming calls.
    /// </summary>
    public class IncomingCallHandler : CallHandler
    {
        private string endpointId;

        private int promptTimes;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncomingCallHandler"/> class.
        /// </summary>
        /// <param name="bot">The bot.</param>
        /// <param name="call">The call.</param>
        /// <param name="endpointId">The bot endpoint id.</param>
        public IncomingCallHandler(Bot bot, ICall call, string endpointId)
            : base(bot, call)
        {
            this.endpointId = endpointId;
        }

        /// <inheritdoc/>
        protected override void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            if (sender.Resource.State == CallState.Established)
            {
                var currentPromptTimes = Interlocked.Increment(ref this.promptTimes);

                if (currentPromptTimes == 1)
                {
                    this.PlayNotificationPrompt();
                }
            }
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
                    var mediaName = this.endpointId == null ? Bot.BotIncomingPromptName : Bot.BotEndpointIncomingPromptName;

                    await this.Call.PlayPromptAsync(new List<MediaPrompt> { this.Bot.MediaMap[mediaName] }).ConfigureAwait(false);
                    this.GraphLogger.Info("Started playing notification prompt");
                }
                catch (Exception ex)
                {
                    this.GraphLogger.Error(ex, $"Failed to play notification prompt.");
                    throw;
                }
            });
        }
    }
}
