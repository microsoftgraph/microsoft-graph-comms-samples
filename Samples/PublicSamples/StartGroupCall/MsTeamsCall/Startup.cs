using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Threading.Tasks;
using CseSample.Services;

[assembly: FunctionsStartup(typeof(CseSample.Startup))]
namespace CseSample
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration _configuration;
        public Startup()
        {
            var configurationBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
            _configuration = configurationBuilder.Build();
        }

        // Utilize dependency injection
        // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // For handling connection well, we utilize Singleton life time
            // https://github.com/Azure/azure-functions-host/wiki/Managing-Connections
            builder.Services.AddSingleton((s) =>
            {
                return CreateIdentityClient();
            });
            builder.Services.AddSingleton<ITokenService, TokenService>();
            builder.Services.AddSingleton<IGraphServiceClient>(new GraphServiceClient(new DelegateAuthenticationProvider(da => Task.FromResult(0))));
            builder.Services.AddScoped<ICallService, CallService>();
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IMeetingService, MeetingService>();
        }

        private IConfidentialClientApplication CreateIdentityClient()
        {
            try
            {
                string clientId = _configuration.GetValue<string>("ClientId");
                string clientSecret = _configuration.GetValue<string>("ClientSecret");

                return ConfidentialClientApplicationBuilder.Create(clientId)
                                          .WithClientSecret(clientSecret)
                                          .Build();
            }
            catch (MsalClientException ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}