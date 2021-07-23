// <copyright file="IBotService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using PsiBot.Model.Models;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Interface IBotService
    /// Implements the <see cref="Model.Contracts.IInitializable" />
    /// </summary>
    /// <seealso cref="Model.Contracts.IInitializable" />
    public interface IBotService
    {
        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        ConcurrentDictionary<string, CallHandler> CallHandlers { get; }

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        ICommunicationsClient Client { get; }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        Task EndCallByCallLegIdAsync(string callLegId);

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);

        void Initialize();
    }
}
