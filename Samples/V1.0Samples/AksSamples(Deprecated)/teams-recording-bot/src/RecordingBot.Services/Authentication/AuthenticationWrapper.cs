// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="AuthenticationWrapper.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************>

using Microsoft.Graph;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecordingBot.Services.Authentication
{
    /// <summary>
    /// A wrapper for the <see cref="IRequestAuthenticationProvider" />
    /// that maps to the <see cref="IAuthenticationProvider" />.
    /// </summary>
    /// <seealso cref="IRequestAuthenticationProvider" />
    /// <seealso cref="IAuthenticationProvider" />
    public class AuthenticationWrapper : IRequestAuthenticationProvider, IAuthenticationProvider
    {
        /// <summary>
        /// The authentication provider
        /// </summary>
        private readonly IRequestAuthenticationProvider authenticationProvider;
        /// <summary>
        /// The tenant
        /// </summary>
        private readonly string tenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationWrapper" /> class.
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
        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            return this.AuthenticateOutboundRequestAsync(request, this.tenant);
        }
    }
}
