// <copyright file="AuthenticationWrapper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Authentication
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Models;
    using Microsoft.Kiota.Abstractions;
    using Microsoft.Kiota.Abstractions.Authentication;

    /// <summary>
    /// A wrapper for the <see cref="IRequestAuthenticationProvider"/>
    /// that maps to the <see cref="IAuthenticationProvider"/>.
    /// </summary>
    /// <seealso cref="IRequestAuthenticationProvider" />
    /// <seealso cref="IAuthenticationProvider" />
    public class AuthenticationWrapper : IRequestAuthenticationProvider, IAuthenticationProvider
    {
        private readonly IRequestAuthenticationProvider authenticationProvider;
        private readonly string tenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationWrapper"/> class.
        /// </summary>
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="tenant">The tenant.</param>
        public AuthenticationWrapper(IRequestAuthenticationProvider authenticationProvider, string tenant = null)
        {
            this.authenticationProvider = authenticationProvider.NotNull(nameof(authenticationProvider));
            this.tenant = tenant;
        }

        /// <inheritdoc />
        public Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
        {
            return this.authenticationProvider.AuthenticateOutboundRequestAsync(request, tenant);
        }

        /// <inheritdoc />
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            return this.authenticationProvider.ValidateInboundRequestAsync(request);
        }

        /// <inheritdoc />
        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
