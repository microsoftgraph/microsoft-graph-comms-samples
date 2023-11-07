// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="IBotService.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using RecordingBot.Model.Models;
using RecordingBot.Services.Bot;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RecordingBot.Services.Contract
{
    /// <summary>
    /// Interface IBotService
    /// Implements the <see cref="RecordingBot.Model.Contracts.IInitializable" />
    /// </summary>
    /// <seealso cref="RecordingBot.Model.Contracts.IInitializable" />
    public interface IBotService : Model.Contracts.IInitializable
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
    }
}
