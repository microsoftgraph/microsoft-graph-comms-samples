// <copyright file="BaseReferenceCountingService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot.Grouping
{
    using System.Threading;

    /// <summary>
    /// Base class that helps maintain reference count for group handlers
    /// to correctly manage the lifecycle of the underlying implementation.
    /// </summary>
    public abstract class BaseReferenceCountingService
    {
        private int referenceCounter;

        /// <summary>
        /// Increment the reference counter.
        /// </summary>
        public void Register()
        {
            Interlocked.Increment(ref this.referenceCounter);
        }

        /// <summary>
        /// Decrements the counter (i.e., de-registers) and returns true
        /// if the count is zero.
        /// </summary>
        /// <returns><see langword="false"/> if the count is 0 after decrementing, otherwise <see langword="false"/>.</returns>
        protected bool DecrementReferenceCount()
        {
            return Interlocked.Decrement(ref this.referenceCounter) == 0;
        }
    }
}
