// <copyright file="AuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.Common.Authentication
{
    using System;
    using System.Diagnostics;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// The authentication provider for this bot instance.
    /// </summary>
    /// <seealso cref="IRequestAuthenticationProvider" />
    public class AuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {
        private const string Resource = "https://graph.microsoft.com";
        private const string DefaultScope = Resource + "/.default";
        private static readonly string[] Scopes = new[] { DefaultScope };

        /// <summary>
        /// The application name.
        /// </summary>
        private readonly string appName;

        /// <summary>
        /// The application identifier.
        /// </summary>
        private readonly string appId;

        /// <summary>
        /// The application secret.
        /// </summary>
        private readonly string appSecret;

        /// <summary>
        /// The application certificate.
        /// </summary>
        private readonly X509Certificate2 appCert;

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
            this.appName = appName.NotNullOrWhitespace(nameof(appName));
            this.appId = appId.NotNullOrWhitespace(nameof(appId));
            this.appSecret = appSecret.NotNullOrWhitespace(nameof(appSecret));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProvider" /> class.
        /// </summary>
        /// <param name="appName">The application name.</param>
        /// <param name="appId">The application identifier.</param>
        /// <param name="appCert">The application certificate.</param>
        /// <param name="logger">The logger.</param>
        public AuthenticationProvider(string appName, string appId, X509Certificate2 appCert, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(AuthenticationProvider)))
        {
            this.appName = appName.NotNullOrWhitespace(nameof(appName));
            this.appId = appId.NotNullOrWhitespace(nameof(appId));
            this.appCert = appCert.NotNull(nameof(appCert));
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
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
        {
            const string Schema = "Bearer";

            tenant = string.IsNullOrWhiteSpace(tenant) ? "common" : tenant;
            var options = new ConfidentialClientApplicationOptions
            {
                ClientName = this.appName,
                ClientId = this.appId,
                ClientVersion = this.GetType().Assembly.GetName().Version.ToString(),
                TenantId = tenant,
                LogLevel = LogLevel.Info,
                EnablePiiLogging = false,
                IsDefaultPlatformLoggingEnabled = false,
                AzureCloudInstance = AzureCloudInstance.AzurePublic,
            };

            ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                .CreateWithApplicationOptions(options)
                .WithLogging(this.LogCallback);
            IConfidentialClientApplication app = string.IsNullOrEmpty(this.appSecret)
                ? builder.WithCertificate(this.appCert).Build()
                : builder.WithClientSecret(this.appSecret).Build();

            AuthenticationResult result = await this.AcquireTokenWithRetryAsync(app, 3).ConfigureAwait(false);

            request.Headers.Authorization = new AuthenticationHeaderValue(Schema, result.AccessToken);
        }

        /// <summary>
        /// Validates the request asynchronously.
        /// This method will be called any time we have an incoming request.
        /// Returning invalid result will trigger a Forbidden response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// The <see cref="RequestValidationResult" /> structure.
        /// </returns>
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
            const string authDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
            if (this.openIdConfiguration == null || DateTime.Now > this.prevOpenIdConfigUpdateTimestamp.Add(this.openIdConfigRefreshInterval))
            {
                this.GraphLogger.Info("Updating OpenID configuration");

                // Download the OIDC configuration which contains the JWKS
                IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        authDomain,
                        new OpenIdConnectConfigurationRetriever());
                this.openIdConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);

                this.prevOpenIdConfigUpdateTimestamp = DateTime.Now;
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
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidIssuers = authIssuers,
                ValidAudience = this.appId,
                IssuerSigningKeys = this.openIdConfiguration.SigningKeys,
            };

            ClaimsPrincipal claimsPrincipal;
            try
            {
                // Now validate the token. If the token is not valid for any reason, an exception will be thrown by the method
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
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
                this.GraphLogger.Error(ex, $"Failed to validate token for client: {this.appId}.");
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
        /// Callback delegate that allows application developers to consume logs, and handle them in a custom manner. This
        /// callback is set using Microsoft.Identity.Client.AbstractApplicationBuilder`1.WithLogging(Microsoft.Identity.Client.LogCallback,System.Nullable{Microsoft.Identity.Client.LogLevel},System.Nullable{System.Boolean},System.Nullable{System.Boolean}).
        /// If PiiLoggingEnabled is set to true, when registering the callback this method will receive the messages twice:
        /// once with the containsPii parameter equals false and the message without PII,
        /// and a second time with the containsPii parameter equals to true and the message might contain PII.
        /// In some cases (when the message does not contain PII), the message will be the same.
        /// For details see https://aka.ms/msal-net-logging.
        /// </summary>
        /// <param name="logLevel">Log level of the log message to process.</param>
        /// <param name="message">Pre-formatted log message.</param>
        /// <param name="containsPii">
        /// Indicates if the log message contains Organizational Identifiable Information (OII)
        /// or Personally Identifiable Information (PII) nor not.
        /// If Microsoft.Identity.Client.Logger.PiiLoggingEnabled is set to false then this value is always false.
        /// Otherwise it will be true when the message contains PII.
        /// </param>
        private void LogCallback(LogLevel logLevel, string message, bool containsPii)
        {
            TraceLevel level;
            switch (logLevel)
            {
                case LogLevel.Error:
                    level = TraceLevel.Error;
                    break;
                case LogLevel.Warning:
                    level = TraceLevel.Warning;
                    break;
                case LogLevel.Info:
                    level = TraceLevel.Info;
                    break;
                default:
                    level = TraceLevel.Verbose;
                    break;
            }

            this.GraphLogger.Log(level, message, nameof(IConfidentialClientApplication));
        }

        /// <summary>
        /// Acquires the token and retries if failure occurs.
        /// </summary>
        /// <param name="app">The confidential application.</param>
        /// <param name="attempts">The attempts.</param>
        /// <returns>
        /// The <see cref="AuthenticationResult" />.
        /// </returns>
        private async Task<AuthenticationResult> AcquireTokenWithRetryAsync(IConfidentialClientApplication app, int attempts)
        {
            while (true)
            {
                attempts--;

                try
                {
                    return await app
                        .AcquireTokenForClient(Scopes)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (attempts < 1)
                    {
                        throw;
                    }
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}