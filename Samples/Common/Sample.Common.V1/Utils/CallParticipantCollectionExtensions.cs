// <copyright file="CallParticipantCollectionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Utils
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Models;

    /// <summary>
    /// The call extensions.
    /// </summary>
    public static class CallParticipantCollectionExtensions
    {
        /// <summary>
        /// Waits for the specified participant to join the call asynchronously.
        /// </summary>
        /// <param name="participants">The participants collection.</param>
        /// <param name="match">The match function.</param>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns>
        /// The joined <see cref="IParticipant" />.
        /// </returns>
        public static Task<IParticipant> WaitForParticipantAsync(
            this IParticipantCollection participants,
            Func<IParticipant, bool> match,
            string failureMessage = null,
            TimeSpan timeOut = default(TimeSpan))
        {
            failureMessage =
                failureMessage
                ?? $"Timed out while waiting for participant in collection {participants.ResourcePath}";

            return participants.WaitForUpdateAsync<IParticipantCollection, IParticipant, Participant>(
                args => args.AddedResources.FirstOrDefault(match),
                failureMessage,
                timeOut);
        }

        /// <summary>
        /// Waits for the specified participant to join the call asynchronously.
        /// </summary>
        /// <param name="participants">The participants collection.</param>
        /// <param name="participantId">The participant identifier.</param>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns>
        /// The joined <see cref="IParticipant" />.
        /// </returns>
        public static Task<IParticipant> WaitForParticipantAsync(
            this IParticipantCollection participants,
            string participantId,
            string failureMessage = null,
            TimeSpan timeOut = default(TimeSpan))
        {
            failureMessage =
                failureMessage
                ?? $"Timed out while waiting for participant {participantId} in collection {participants.ResourcePath}";

            return participants.WaitForParticipantAsync(
                participant => participantId.EqualsIgnoreCase(participant.Resource.Id),
                failureMessage,
                timeOut);
        }

        /// <summary>
        /// Waits for the specified participant to be removed from the call asynchronously.
        /// </summary>
        /// <param name="participants">The participants collection.</param>
        /// <param name="match">The match function.</param>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns>
        /// The removed <see cref="IParticipant" />.
        /// </returns>
        public static Task<IParticipant> WaitForRemovedParticipantAsync(
            this IParticipantCollection participants,
            Func<IParticipant, bool> match,
            string failureMessage = null,
            TimeSpan timeOut = default(TimeSpan))
        {
            failureMessage =
                failureMessage
                ?? $"Timed out while waiting for participant to be removed from collection {participants.ResourcePath}";

            return participants.WaitForUpdateAsync<IParticipantCollection, IParticipant, Participant>(
                args => args.RemovedResources.FirstOrDefault(match),
                failureMessage,
                timeOut);
        }
    }
}
