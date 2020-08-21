using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace CseSample.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfidentialClientApplication _confidentialClient;

        public TokenService(IConfidentialClientApplication confidentialClient)
        {
            _confidentialClient = confidentialClient;
        }

        public async Task<string> FetchAccessTokenByTenantId(string tenantId)
        {
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            // AcquireTokenForClient().ExecuteAsync() hard to mock
            // Because AcquireTokenForClient() returns sealed class, so we can't mock it and it's ExecuteAsync()
            var result = await _confidentialClient.AcquireTokenForClient(scopes)
                            .WithAuthority($"https://login.microsoftonline.com/{tenantId}").ExecuteAsync();
            return result.AccessToken;
        }
    }
}