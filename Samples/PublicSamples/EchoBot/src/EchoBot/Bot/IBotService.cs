// ***********************************************************************
// Assembly         : EchoBot.Bot
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 10-17-2023
// ***********************************************************************
// <copyright file="IBotService.cs" company="Microsoft">
//     Copyright ©  2023
// </copyrigh>t
// <summary></summary>
// ***********************************************************************
using EchoBot.Models;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using System.Collections.Concurrent;

namespace EchoBot.Bot
{
    /// <summary>
    /// Interface IBotService
    /// </summary>
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
        /// <param name="threadId">The thread id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        Task EndCallByThreadIdAsync(string threadId);

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);

        /// <summary>
        /// Initialize the bot instance
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shutdown the bot instance
        /// </summary>
        /// <returns></returns>
        Task Shutdown();
    }
}

