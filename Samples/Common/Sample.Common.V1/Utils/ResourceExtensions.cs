// <copyright file="ResourceExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Graph.Models;

    /// <summary>
    /// Resource extensions for testing.
    /// </summary>
    public static class ResourceExtensions
    {
        /// <summary>
        /// Waits for the matching update asynchronously.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="match">The match.</param>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        public static async Task WaitForUpdateAsync<TResource, TEntity>(
            this TResource resource,
            Func<ResourceEventArgs<TEntity>, bool> match,
            string failureMessage = null,
            TimeSpan timeout = default(TimeSpan))
            where TResource : IResource<TResource, TEntity>
            where TEntity : Entity
        {
            failureMessage =
                failureMessage
                ?? $"Timed out while waiting for update in {resource.ResourcePath}.";

            if (timeout == TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(60);
            }

            var matchedTcs = new TaskCompletionSource<bool>();

            void ResourceOnUpdated(TResource sender, ResourceEventArgs<TEntity> e)
            {
                if (match(e))
                {
                    matchedTcs.TrySetResult(true);
                }
            }

            resource.OnUpdated += ResourceOnUpdated;

            var eventArgs = new ResourceEventArgs<TEntity>(null, resource.Resource, resource.ResourcePath);

            try
            {
                // Check if there is a match with the current resource.
                if (match(eventArgs))
                {
                    return;
                }

                await matchedTcs.Task.ValidateAsync(
                    timeout,
                    failureMessage).ConfigureAwait(false);
            }
            finally
            {
                resource.OnUpdated -= ResourceOnUpdated;
            }
        }

        /// <summary>
        /// Waits for the matching update asynchronously.
        /// </summary>
        /// <typeparam name="TResourceCollection">The type of the resource collection.</typeparam>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="resourceCollection">The resource collection.</param>
        /// <param name="match">The match.</param>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// The matching <see cref="IResource{TSelf,TEntity}" />.
        /// </returns>
        public static async Task<TResource> WaitForUpdateAsync<TResourceCollection, TResource, TEntity>(
            this TResourceCollection resourceCollection,
            Func<CollectionEventArgs<TResource>, TResource> match,
            string failureMessage = null,
            TimeSpan timeout = default(TimeSpan))
            where TResourceCollection : IResourceCollection<TResourceCollection, TResource, TEntity>
            where TResource : IResource<TResource, TEntity>
            where TEntity : Entity
        {
            failureMessage =
                failureMessage
                ?? $"Timed out while waiting for update in collection {resourceCollection.ResourcePath}.";

            if (timeout == TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(60);
            }

            var matchedTcs = new TaskCompletionSource<TResource>();
            void OnUpdated(TResourceCollection sender, CollectionEventArgs<TResource> e)
            {
                var resource = match(e);
                if (resource != null)
                {
                    matchedTcs.TrySetResult(resource);
                }
            }

            resourceCollection.OnUpdated += OnUpdated;

            var existingResources = new List<TResource>(resourceCollection);
            var collectionEventArgs = new CollectionEventArgs<TResource>(resourceCollection.ResourcePath, addedResources: existingResources);

            try
            {
                // Check if there is a match with the current collection.
                var resource = match(collectionEventArgs);
                if (resource != null)
                {
                    return resource;
                }

                return await matchedTcs.Task.ValidateAsync(
                    timeout,
                    failureMessage).ConfigureAwait(false);
            }
            finally
            {
                resourceCollection.OnUpdated -= OnUpdated;
            }
        }
    }
}
