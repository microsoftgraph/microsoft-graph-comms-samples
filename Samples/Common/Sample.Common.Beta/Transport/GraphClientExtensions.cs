// <copyright file="GraphClientExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Transport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Transport;
    using Microsoft.Kiota.Abstractions;

    /// <summary>
    /// Extensions for <see cref="IGraphClient"/>.
    /// </summary>
    public static class GraphClientExtensions
    {
        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IGraphResponse{T}" />.
        /// </returns>
        public static Task<IGraphResponse<TResponse>> SendAsync<TRequest, TResponse>(
            this IGraphClient client,
            IGraphRequest<TRequest> request,
            string tenant,
            Guid scenarioId,
            CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : class
            where TResponse : class
        {
            if (!string.IsNullOrWhiteSpace(tenant))
            {
                request.Properties.Add(GraphProperty.Property(HttpConstants.HeaderNames.Tenant, tenant));
            }

            request.Properties.Add(GraphProperty.RequestProperty(HttpConstants.HeaderNames.ScenarioId, scenarioId));
            request.Properties.Add(GraphProperty.RequestProperty(HttpConstants.HeaderNames.ClientRequestId, Guid.NewGuid()));

            return client.SendAsync<TRequest, TResponse>(request, cancellationToken);
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IGraphResponse{T}" />.
        /// </returns>
        public static Task<IGraphResponse> SendAsync<TRequest>(
            this IGraphClient client,
            IGraphRequest<TRequest> request,
            string tenant,
            Guid scenarioId,
            CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : class
        {
            if (!string.IsNullOrWhiteSpace(tenant))
            {
                request.Properties.Add(GraphProperty.Property(HttpConstants.HeaderNames.Tenant, tenant));
            }

            request.Properties.Add(GraphProperty.RequestProperty(HttpConstants.HeaderNames.ScenarioId, scenarioId));
            request.Properties.Add(GraphProperty.RequestProperty(HttpConstants.HeaderNames.ClientRequestId, Guid.NewGuid()));

            return client.SendAsync<TRequest>(request, cancellationToken);
        }

        /// <summary>
        /// Sends the request asynchronously.
        /// </summary>
        /// <typeparam name="T"><see cref="Type" /> of the content present in the response.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="requestType">Type of the request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task" /> returning the generic type.
        /// </returns>
        public static async Task<T> SendAsync<T>(
            this IGraphClient client,
            IGraphRequest<object> request,
            RequestType requestType,
            string tenant,
            Guid scenarioId,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            var graphResponse = await client
                .SendAsync<object, T>(request, tenant, scenarioId, cancellationToken)
                .ConfigureAwait(false);
            return graphResponse.Content;
        }

        /// <summary>
        /// Sends the request asynchronously.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="requestType">Type of the request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        public static Task SendAsync(
            this IGraphClient client,
            IGraphRequest<object> request,
            RequestType requestType,
            string tenant,
            Guid scenarioId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return client.SendAsync<NoContentMessage>(request, requestType, tenant, scenarioId, cancellationToken);
        }
    }
}
