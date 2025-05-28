// <copyright file="CustomAuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Authentication
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Identity.Client;
    using Sample.Common;

    /// <summary>
    /// Custom implementation of IRequestAuthenticationProvider that properly handles tenant ID.
    /// </summary>
    public class CustomAuthenticationProvider : IRequestAuthenticationProvider
    {
        private readonly string appId;
        private readonly string appSecret;
        private readonly IGraphLogger logger;
        private readonly string tenantId;

        // Add a lock object for thread safety when modifying the client application
        private readonly object lockObject = new object();

        // Remove readonly to allow reassignment
        private IConfidentialClientApplication confidentialClientApplication;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="appId">The application ID.</param>
        /// <param name="appSecret">The application secret.</param>
        /// <param name="logger">The logger.</param>
        public CustomAuthenticationProvider(string appId, string appSecret, IGraphLogger logger)
        {
            this.appId = appId;
            this.appSecret = appSecret;
            this.logger = logger;

            // Default to organizations
            this.tenantId = "organizations";

            try
            {
                // Get the tenant ID directly from the parameters
                // We already have the appId and appSecret, so we don't need to access the configuration again
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Using default tenant ID: {this.tenantId}",
                    EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Error setting up tenant ID: {ex.Message}. Using default 'organizations'.",
                    EventLogEntryType.Warning);
            }

            // Create the MSAL confidential client application
            this.confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithClientSecret(appSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{this.tenantId}"))
                .Build();

            EventLog.WriteEntry(
                "AudioVideoPlaybackService",
                $"CustomAuthenticationProvider initialized with AppId: {appId} and TenantId: {this.tenantId}",
                EventLogEntryType.Information);
        }

        /// <summary>
        /// Authenticates the outbound request with the specified tenant.
        /// </summary>
        /// <param name="request">The request to authenticate.</param>
        /// <param name="tenant">The tenant.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
        {
            return this.AuthenticateOutboundRequestAsync(request);
        }

        /// <summary>
        /// Authenticates the outbound request.
        /// </summary>
        /// <param name="request">The request to authenticate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request)
        {
            try
            {
                // Use the Graph scope for Microsoft Graph API
                string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

                // Add retry logic for handling transient errors
                int retryCount = 0;
                const int maxRetries = 3;
                bool success = false;
                Exception lastException = null;

                while (!success && retryCount < maxRetries)
                {
                    try
                    {
                        // Log the current attempt
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Attempting to acquire token (attempt {retryCount + 1} of {maxRetries})",
                            EventLogEntryType.Information);

                        // Acquire token
                        var authResult = await this.confidentialClientApplication
                            .AcquireTokenForClient(scopes)
                            .WithSendX5C(true) // Include the X5C header for certificate-based authentication
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                        // Log token details (but not the actual token for security)
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Token acquired successfully. Expires: {authResult.ExpiresOn}, Tenant: {authResult.TenantId}",
                            EventLogEntryType.Information);

                        // Add the token to the Authorization header
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                        // Log the request details
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Request authenticated: {request.Method} {request.RequestUri}",
                            EventLogEntryType.Information);

                        success = true;
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        lastException = ex;

                        // This is a Conditional Access policy error
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Conditional Access policy blocking token issuance: {ex.Message}",
                            EventLogEntryType.Warning);

                        // For service applications, we can't prompt for user interaction
                        // Instead, we'll try with a different authority
                        lock (this.lockObject)
                        {
                            if (retryCount == 0)
                            {
                                // Try with specific tenant ID
                                EventLog.WriteEntry(
                                    "AudioVideoPlaybackService",
                                    $"Retrying with specific tenant ID: {this.tenantId}",
                                    EventLogEntryType.Information);

                                this.confidentialClientApplication = this.CreateClientApplication(this.tenantId);
                            }
                            else if (retryCount == 1)
                            {
                                // Try with common endpoint
                                EventLog.WriteEntry(
                                    "AudioVideoPlaybackService",
                                    "Retrying with common endpoint",
                                    EventLogEntryType.Information);

                                this.confidentialClientApplication = this.CreateClientApplication("common");
                            }
                            else
                            {
                                // We've tried all options, rethrow the exception
                                throw;
                            }
                        }

                        retryCount++;
                    }
                    catch (MsalServiceException ex)
                    {
                        lastException = ex;

                        // This is a service-level exception
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"MSAL Service Exception: {ex.Message}, ErrorCode: {ex.ErrorCode}, StatusCode: {ex.StatusCode}",
                            EventLogEntryType.Error);

                        // If it's a transient error, retry
                        if (this.IsTransientError(ex))
                        {
                            retryCount++;
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))).ConfigureAwait(false); // Exponential backoff
                        }
                        else
                        {
                            // Non-transient error, rethrow
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;

                        // For other exceptions, log and retry with backoff
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Error authenticating request: {ex.Message}, Type: {ex.GetType().Name}",
                            EventLogEntryType.Error);

                        retryCount++;
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount))).ConfigureAwait(false); // Exponential backoff
                    }
                }

                if (!success)
                {
                    // If we've exhausted all retries, throw the last exception
                    EventLog.WriteEntry(
                        "AudioVideoPlaybackService",
                        "Failed to authenticate request after multiple attempts",
                        EventLogEntryType.Error);

                    throw lastException ?? new InvalidOperationException("Failed to authenticate request after multiple attempts");
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Error in AuthenticateOutboundRequestAsync: {ex.Message}\nStack trace: {ex.StackTrace}",
                    EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Gets the authentication token for the specified tenant.
        /// </summary>
        /// <param name="tenant">The tenant.</param>
        /// <returns>A task that resolves to the authentication token.</returns>
        public Task<string> GetAuthenticationTokenAsync(string tenant)
        {
            // Always use the common endpoint
            return this.GetAuthenticationTokenAsync();
        }

        /// <summary>
        /// Gets the authentication token.
        /// </summary>
        /// <returns>A task that resolves to the authentication token.</returns>
        public async Task<string> GetAuthenticationTokenAsync()
        {
            try
            {
                string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

                // Add retry logic for handling transient errors
                int retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        var authResult = await this.confidentialClientApplication
                            .AcquireTokenForClient(scopes)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            "Successfully acquired authentication token",
                            EventLogEntryType.Information);

                        return authResult.AccessToken;
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        // This is a Conditional Access policy error
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Conditional Access policy blocking token issuance: {ex.Message}",
                            EventLogEntryType.Warning);

                        // For service applications, we can't prompt for user interaction
                        // Instead, we'll try with a different authority
                        lock (this.lockObject)
                        {
                            if (retryCount == 0)
                            {
                                // Try with organizations authority
                                this.confidentialClientApplication = this.CreateClientApplication("organizations");
                            }
                            else if (retryCount == 1)
                            {
                                // Try with consumers authority
                                this.confidentialClientApplication = this.CreateClientApplication("consumers");
                            }
                            else
                            {
                                // We've tried all options, rethrow the exception
                                throw;
                            }
                        }

                        retryCount++;
                    }
                    catch (Exception ex)
                    {
                        // For other exceptions, log and rethrow
                        EventLog.WriteEntry(
                            "AudioVideoPlaybackService",
                            $"Error getting authentication token: {ex.Message}",
                            EventLogEntryType.Error);
                        throw;
                    }
                }

                // If we get here, all retries failed
                throw new InvalidOperationException("Failed to acquire authentication token after multiple attempts");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Error getting authentication token: {ex.Message}",
                    EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Validates the inbound request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>A task that resolves to the validation result.</returns>
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            // For this sample, we're not validating inbound requests
            // In a production environment, you would validate the token in the Authorization header
            this.logger.Info($"Inbound request validation skipped for {request.RequestUri}");

            // Return a successful validation result
            return Task.FromResult(new RequestValidationResult
            {
                IsValid = true,
            });
        }

        /// <summary>
        /// Determines if the exception represents a transient error that can be retried.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the error is transient, false otherwise.</returns>
        private bool IsTransientError(MsalServiceException ex)
        {
            // Check for specific error codes that indicate transient errors
            return ex.StatusCode == 429 || // Too many requests
                   ex.StatusCode == 503 || // Service unavailable
                   ex.StatusCode == 504 || // Gateway timeout
                   ex.ErrorCode == "temporarily_unavailable" ||
                   ex.Message.Contains("AADSTS50196"); // Temporary server condition
        }

        /// <summary>
        /// Creates a new confidential client application with the specified authority.
        /// </summary>
        /// <param name="authority">The authority to use.</param>
        /// <returns>The new confidential client application.</returns>
        private IConfidentialClientApplication CreateClientApplication(string authority)
        {
            EventLog.WriteEntry(
                "AudioVideoPlaybackService",
                $"Creating new client application with authority: {authority}",
                EventLogEntryType.Information);

            return ConfidentialClientApplicationBuilder
                .Create(this.appId)
                .WithClientSecret(this.appSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{authority}"))
                .Build();
        }
    }
}