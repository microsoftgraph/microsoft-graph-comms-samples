// <copyright file="RequestAuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.OnlineMeeting
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// The authentication provider.
    /// </summary>
    /// <seealso cref="IRequestAuthenticationProvider" />
    public class RequestAuthenticationProvider : IRequestAuthenticationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="appId">The application identifier.</param>
        /// <param name="appSecret">The application secret.</param>
        public RequestAuthenticationProvider(string appId, string appSecret)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(appId), $"Invalid {nameof(appId)}.");
            Debug.Assert(!string.IsNullOrWhiteSpace(appSecret), $"Invalid {nameof(appSecret)}.");

            this.AppId = appId;
            this.AppSecret = appSecret;
        }

        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        /// <value>
        /// The application identifier.
        /// </value>
        private string AppId { get; }

        /// <summary>
        /// Gets the application secret.
        /// </summary>
        /// <value>
        /// The application secret.
        /// </value>
        private string AppSecret { get; }

        /// <inheritdoc />
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenantId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(tenantId), $"Invalid {nameof(tenantId)}.");

            const string BearerPrefix = "Bearer";
            const string ReplaceString = "{tenant}";
            const string TokenAuthorityMicrosoft = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            const string Resource = @"https://graph.microsoft.com";

            var tokenLink = TokenAuthorityMicrosoft.Replace(ReplaceString, tenantId);

            var context = new AuthenticationContext(tokenLink);
            var creds = new ClientCredential(this.AppId, this.AppSecret);

            AuthenticationResult result;
            try
            {
                result = await context.AcquireTokenAsync(Resource, creds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.Write($"Acquire token failed {ex.Message}");

                throw;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(BearerPrefix, result.AccessToken);
        }
    }
}