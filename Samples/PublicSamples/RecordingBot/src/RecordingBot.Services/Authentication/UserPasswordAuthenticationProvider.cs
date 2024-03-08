using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RecordingBot.Services.Authentication
{
    public class UserPasswordAuthenticationProvider : ObjectRoot, IRequestAuthenticationProvider
    {
        private readonly string appName;
        private readonly string appId;
        private readonly string appSecret;
        private readonly string userName;
        private readonly string password;

        public UserPasswordAuthenticationProvider(string appName, string appId, string appSecret, string userName, string password, IGraphLogger logger)
            : base(logger.NotNull(nameof(logger)).CreateShim(nameof(UserPasswordAuthenticationProvider)))
        {
            this.appName = appName.NotNullOrWhitespace(nameof(appName));
            this.appId = appId.NotNullOrWhitespace(nameof(appId));
            this.appSecret = appSecret.NotNullOrWhitespace(nameof(appSecret));

            this.userName = userName.NotNullOrWhitespace(nameof(userName));
            this.password = password.NotNullOrWhitespace(nameof(password));
        }

        /// <inheritdoc />
        public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenantId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(tenantId), $"Invalid {nameof(tenantId)}.");

            const string BearerPrefix = "Bearer";
            const string ReplaceString = "{tenant}";
            const string TokenAuthorityMicrosoft = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
            const string Resource = @"https://graph.microsoft.com/.default";

            var tokenLink = TokenAuthorityMicrosoft.Replace(ReplaceString, tenantId);
            OAuthResponse authResult = null;

            try
            {
                using var httpClient = new HttpClient();
                var result = await httpClient.PostAsync(tokenLink, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", userName),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("scope", Resource),
                    new KeyValuePair<string, string>("client_id", appId),
                    new KeyValuePair<string, string>("client_secret", appSecret),
                })).ConfigureAwait(false);

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to generate user token.");
                }

                var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                authResult = JsonConvert.DeserializeObject<OAuthResponse>(content);

                request.Headers.Authorization = new AuthenticationHeaderValue(BearerPrefix, authResult.Access_Token);
            }
            catch (Exception ex)
            {
                GraphLogger.Error(ex, $"Failed to generate user token for user: {userName}");
                throw;
            }

            GraphLogger.Info($"Generated OAuth token. Expires in {authResult.Expires_In / 60}  minutes.");
        }

        /// <inheritdoc />
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            // Currently no scenarios on /user | /me path for inbound requests.
            throw new NotImplementedException();
        }

        private class OAuthResponse
        {
            public string Access_Token { get; set; }
            public int Expires_In { get; set; }
        }
    }
}
