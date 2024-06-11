// <copyright file="TaskExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Extensions for asyncronous <see cref="Task"/> management.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Validates the asynchronous.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="Task" />.</returns>
        public static async Task ValidateAsync(this Task task, TimeSpan delay, string message = null)
        {
            var delayTask = Task.Delay(delay);
            var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
            Assert.AreEqual(task, completedTask, message ?? "Validating Task timed out.");
        }

        /// <summary>
        /// Validates the asynchronous.
        /// </summary>
        /// <typeparam name="T">The task return value type.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="Task{T}" />.</returns>
        public static async Task<T> ValidateAsync<T>(this Task<T> task, TimeSpan delay, string message = null)
        {
            var delayTask = Task.Delay(delay);
            var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
            if (completedTask == delayTask)
            {
                Assert.Fail(message ?? $"Validating Task<{typeof(T).Name}> timed out.");
            }

            return await task.ConfigureAwait(false);
        }

#pragma warning disable AvoidAsyncVoid // Avoid async void
        /// <summary>
        /// Extension for Task to execute the task in background and log any exception.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="description">The description.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="lineNumber">The line number.</param>
        public static async void ForgetAndLogException(
            this Task task,
            IGraphLogger logger,
            string description = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                description = string.IsNullOrWhiteSpace(description)
                    ? "Exception while executing task."
                    : description;

                logger.Error(
                    ex,
                    description,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);
            }
        }
#pragma warning restore AvoidAsyncVoid // Avoid async void
    }
}
