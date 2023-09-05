using EchoBot.Api.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Skype.Bots.Media;

namespace EchoBot.Api.ServiceSetup
{
	public class BotSettings : IBotSettings
	{
        /// <summary>
        /// Gets or sets the call control listening urls.
        /// </summary>
        /// <value>The call control listening urls.</value>
        public string[] CallControlListeningUrls { get; set; }
        
        //public MediaPlatformSettings? MediaPlatformSettings { get; private set; }

        //private readonly ILogger _logger;
        //private readonly AppSettings _settings;

        //public BotSettings(ILogger<BotSettings> logger, IOptions<AppSettings> settings)
        //{
        //    _logger = logger;
        //    _settings = settings.Value;
        //}



        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public BotSettings(AppSettings settings)
        {
            var botInternalHostingProtocol = "https";
            if (settings.UseLocalDevSettings)
            {
                // if running locally with ngrok
                // the call signalling and notification will use the same internal and external ports
                // because you cannot receive requests on the same tunnel with different ports

                // calls come in over 443 (external) and route to the internally hosted port: BotCallingInternalPort
                settings.BotInstanceExternalPort = 443;
                settings.BotInternalPort = settings.BotCallingInternalPort;
                botInternalHostingProtocol = "http";

                if (string.IsNullOrEmpty(settings.MediaDnsName)) throw new ArgumentNullException(nameof(settings.MediaDnsName));
            }
            else
            {
                settings.MediaDnsName = settings.ServiceDnsName;
            }

            if (string.IsNullOrEmpty(settings.ServiceDnsName)) throw new ArgumentNullException(nameof(settings.ServiceDnsName));
            if (string.IsNullOrEmpty(settings.AadAppId)) throw new ArgumentNullException(nameof(settings.AadAppId));
            if (string.IsNullOrEmpty(settings.AadAppSecret)) throw new ArgumentNullException(nameof(settings.AadAppSecret));
            if (settings.BotCallingInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(settings.BotCallingInternalPort));
            if (settings.BotInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(settings.BotInstanceExternalPort));
            if (settings.BotInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(settings.BotInternalPort));
            if (settings.MediaInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(settings.MediaInstanceExternalPort));
            if (settings.MediaInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(settings.MediaInternalPort));

            // localhost
            var baseDomain = "+";

            // Create structured config objects for service.
            //CallControlBaseUrl = new Uri($"https://{settings.ServiceDnsName}:{settings.BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");

            // http for local development
            // https for running on VM
            var controlListenUris = new HashSet<string>
            {
                $"{botInternalHostingProtocol}://{baseDomain}:{settings.BotCallingInternalPort}/",
                $"{botInternalHostingProtocol}://{baseDomain}:{settings.BotInternalPort}/"
            };

            CallControlListeningUrls = controlListenUris.ToArray();
        }
    }
}

