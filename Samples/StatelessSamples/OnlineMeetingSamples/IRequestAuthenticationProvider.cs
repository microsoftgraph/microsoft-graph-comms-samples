// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRequestAuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The authentication provider interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.OnlineMeeting
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// The authentication provider interface.
    /// </summary>
    public interface IRequestAuthenticationProvider
    {
        /// <summary>
        /// Authenticates the specified request message.
        /// This method will be called any time there is an outbound request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant);
    }
}