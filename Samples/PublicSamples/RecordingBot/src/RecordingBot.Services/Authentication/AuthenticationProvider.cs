// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="AuthenticationProvider.cs" company="Microsoft">
//     Copyright �  2020
// </copyright>
// <summary></summary>
// ***********************************************************************>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.

using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RecordingBot.Model.Constants;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace RecordingBot.Services.Authentication
{
    /// <summary>
    /// The authentication provider for this bot instance.
    /// </summary>
    /// <seealso cref="IRequestAuthenticationProvider" />
    public class AuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {
        /// <summary>
        /// The open ID configuration refresh interval.
        /// </summary>
        private readonly TimeSpan openIdConfigRefreshInterval = TimeSpan.FromHours(2);

        /// <summary>
        /// The previous update timestamp for OpenIdConfig.
        /// </summary>
        private DateTime prevOpenIdConfigUpdateTimestamp = DateTime.MinValue;

        /// <summary>
        /// The open identifier configuration.
        /// </summary>
        private OpenIdConnectConfiguration openIdConfiguration;
        private readonly ConfidentialClientApplicationOptions _clientOptions;
        private static readonly IEnumerable<string> _defaultScopes = new List<string> { "https://graph.microsoft.com/.default" };

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProvider" /> class.
        /// </summary>
        /// <param name="appName">The application name.</param>
        /// <param name="appId">The application identifier.</param>
        /// <param name="appSecret">The application secret.</param>
        /// <param name="logger">The logger.</param>
        public AuthenticationProvider(string appName, string appId, string appSecret, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(AuthenticationProvider)))
        {
            _clientOptions = new ConfidentialClientApplicationOptions
            {
                ClientName = appName.NotNullOrWhitespace(nameof(appName)),
                ClientId = appId.NotNullOrWhitespace(nameof(appId)),
                ClientSecret = appSecret.NotNullOrWhitespace(nameof(appSecret))
            };
        }

        /// <summary>
        /// Authenticates the specified request message.
        /// This method will be called any time there is an outbound request.
        /// In this case we are using the Microsoft.IdentityModel.Clients.ActiveDirectory library
        /// to stamp the outbound http request with the OAuth 2.0 token using an AAD application id
        /// and application secret.  Alternatively, this method can support certificate validation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="tenant">The tenant.</param>
        /// <returns>The <see cref="Task" />.</returns>
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
        {
            const string schema = "Bearer";

            // If no tenant was specified, we craft the token link using the common tenant.
            // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-protocols#endpoints
            tenant = string.IsNullOrWhiteSpace(tenant) ? "common" : tenant;

            this.GraphLogger.Info("AuthenticationProvider: Generating OAuth token.");

            var clientApp = ConfidentialClientApplicationBuilder.CreateWithApplicationOptions(_clientOptions).WithTenantId(tenant).Build();

            AuthenticationResult result;
            try
            {
                result = await this.AcquireTokenWithRetryAsync(clientApp, attempts: 3);
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"Failed to generate token for client: {_clientOptions.ClientId}");
                throw;
            }

            GraphLogger.Info($"AuthenticationProvider: Generated OAuth token. Expires in {result.ExpiresOn.Subtract(DateTimeOffset.UtcNow).TotalMinutes} minutes.");

            request.Headers.Authorization = new AuthenticationHeaderValue(schema, result.AccessToken);
        }

        /// <summary>
        /// Validates the request asynchronously.
        /// This method will be called any time we have an incoming request.
        /// Returning invalid result will trigger a Forbidden response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="RequestValidationResult" /> structure.</returns>
        public async Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            var token = request?.Headers?.Authorization?.Parameter;
            if (string.IsNullOrWhiteSpace(token))
            {
                return new RequestValidationResult { IsValid = false };
            }

            // Currently the service does not sign outbound request using AAD, instead it is signed
            // with a private certificate.  In order for us to be able to ensure the certificate is
            // valid we need to download the corresponding public keys from a trusted source.
            const string authDomain = AzureConstants.AuthDomain;
            if (openIdConfiguration == null || DateTime.Now > prevOpenIdConfigUpdateTimestamp.Add(openIdConfigRefreshInterval))
            {
                GraphLogger.Info("Updating OpenID configuration");

                // Download the OIDC configuration which contains the JWKS
                IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        authDomain,
                        new OpenIdConnectConfigurationRetriever());
                openIdConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);

                prevOpenIdConfigUpdateTimestamp = DateTime.Now;
            }

            // The incoming token should be issued by graph.
            var authIssuers = new[]
            {
                "https://graph.microsoft.com",
                "https://api.botframework.com",
            };

            // Configure the TokenValidationParameters.
            // Aet the Issuer(s) and Audience(s) to validate and
            // assign the SigningKeys which were downloaded from AuthDomain.
            TokenValidationParameters validationParameters = new()
            {
                ValidIssuers = authIssuers,
                ValidAudience = _clientOptions.ClientId,
                IssuerSigningKeys = this.openIdConfiguration.SigningKeys,
            };

            ClaimsPrincipal claimsPrincipal;
            try
            {
                // Now validate the token. If the token is not valid for any reason, an exception will be thrown by the method
                JwtSecurityTokenHandler handler = new();
                claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
            }

            // Token expired... should somehow return 401 (Unauthorized)
            // catch (SecurityTokenExpiredException ex)
            // Tampered token
            // catch (SecurityTokenInvalidSignatureException ex)
            // Some other validation error
            // catch (SecurityTokenValidationException ex)
            catch (Exception ex)
            {
                // Some other error
                this.GraphLogger.Error(ex, $"Failed to validate token for client: {_clientOptions.ClientId}.");
                return new RequestValidationResult() { IsValid = false };
            }

            const string ClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
            var tenantClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals(ClaimType, StringComparison.Ordinal));

            if (string.IsNullOrEmpty(tenantClaim?.Value))
            {
                // No tenant claim given to us.  reject the request.
                return new RequestValidationResult { IsValid = false };
            }

            return new RequestValidationResult { IsValid = true, TenantId = tenantClaim.Value };
        }

        /// <summary>
        /// Acquires the token and retries if failure occurs.
        /// </summary>
        /// <param name="context">The application context.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="creds">The application credentials.</param>
        /// <param name="attempts">The attempts.</param>
        /// <returns>The <see cref="AuthenticationResult" />.</returns>
        private async Task<AuthenticationResult> AcquireTokenWithRetryAsync(IConfidentialClientApplication context, int attempts)
        {
            while (true)
            {
                attempts--;

                try
                {
                    return await context.AcquireTokenForClient(_defaultScopes).ExecuteAsync();
                }
                catch (Exception)
                {
                    if (attempts < 1)
                    {
                        throw;
                    }
                }

                await Task.Delay(1000);
            }
        }
    }
}
