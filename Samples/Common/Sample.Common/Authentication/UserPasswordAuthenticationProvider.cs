// <copyright file="UserPasswordAuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace OnlineMeeting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Newtonsoft.Json;

    /// <summary>
    /// Authentication provider to add .
    /// </summary>
    public class UserPasswordAuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPasswordAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="appId">The application identifier.</param>
        /// <param name="appSecret">The application secret.</param>
        /// <param name="userName">The username to be used.</param>
        /// <param name="password">Password assoicated with the passed username.</param>
        /// <param name="logger">The logger.</param>
        public UserPasswordAuthenticationProvider(string appId, string appSecret, string userName, string password, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(UserPasswordAuthenticationProvider)))
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(appId), $"Invalid {nameof(appId)}.");
            Debug.Assert(!string.IsNullOrWhiteSpace(appSecret), $"Invalid {nameof(appSecret)}.");
            Debug.Assert(!string.IsNullOrWhiteSpace(userName), $"Invalid {nameof(userName)}.");
            Debug.Assert(!string.IsNullOrWhiteSpace(password), $"Invalid {nameof(password)}.");

            this.AppId = appId;
            this.AppSecret = appSecret;

            // NOTE: STORING USERNAME/PASSWORD IN A FILE IS NOT SAFE. THIS SAMPLE IS FOR DEMONSTRATION PURPOSE ONLY.
            this.UserName = userName;
            this.Password = password;
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

        /// <summary>
        /// Gets UserName to be passed to oauth service.
        /// </summary>
        private string UserName { get; }

        /// <summary>
        /// Gets password to be passed to oauth service.
        /// </summary>
        private string Password { get; }

        /// <inheritdoc />
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenantId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(tenantId), $"Invalid {nameof(tenantId)}.");

            const string BearerPrefix = "Bearer";
            const string ReplaceString = "{tenant}";
            const string TokenAuthorityMicrosoft = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            const string Resource = @"https://graph.microsoft.com";

            var tokenLink = TokenAuthorityMicrosoft.Replace(ReplaceString, tenantId);
            OAuthResponse authResult = null;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result1 = await httpClient.PostAsync(tokenLink, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("resource", Resource),
                        new KeyValuePair<string, string>("client_id", this.AppId),
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", this.UserName),
                        new KeyValuePair<string, string>("password", this.Password),
                        new KeyValuePair<string, string>("scope", "openid"),
                        new KeyValuePair<string, string>("client_secret", this.AppSecret),
                    })).ConfigureAwait(false);

                    var content = await result1.Content.ReadAsStringAsync().ConfigureAwait(false);
                    authResult = JsonConvert.DeserializeObject<OAuthResponse>(content);

                    request.Headers.Authorization = new AuthenticationHeaderValue(BearerPrefix, authResult.Access_Token);
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"Failed to generate user token for user: {this.UserName}");
                throw;
            }

            this.GraphLogger.Info($"Generated OAuth token. Expires in {authResult.Expires_In / 60}  minutes.");
        }

        /// <inheritdoc />
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Response received from oauth service.
        /// </summary>
        private class OAuthResponse
        {
            /// <summary>
            /// Gets or Sets access token.
            /// </summary>
            public string Access_Token { get; set; }

            /// <summary>
            ///  Gets or Sets expires time.
            /// </summary>
            public int Expires_In { get; set; }
        }
    }
}
