using Microsoft.Skype.Bots.Media;
using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AVPWindowsService
{
    /// <summary>
    /// Class AzureSettings.
    /// Implements the <see cref="EchoBot.Services.Contract.IAzureSettings" />
    /// </summary>
    /// <seealso cref="EchoBot.Services.Contract.IAzureSettings" />
    public class AzureSettings
    {
        /// <summary>
        /// Gets or sets the name of the bot.
        /// </summary>
        /// <value>The name of the bot.</value>
        public string BotName { get; set; }

        /// <summary>
        /// Gets or sets the name of the service DNS.
        /// </summary>
        /// <value>The name of the service DNS.</value>
        public string ServiceDnsName { get; set; }

        /// <summary>
        /// Gets or sets the service cname.
        /// </summary>
        /// <value>The service cname.</value>
        public string ServiceCname { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>The certificate thumbprint.</value>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the call control listening urls.
        /// </summary>
        /// <value>The call control listening urls.</value>
        public IEnumerable<string> CallControlListeningUrls { get; set; }

        /// <summary>
        /// Gets or sets the call control base URL.
        /// </summary>
        /// <value>The call control base URL.</value>
        public Uri CallControlBaseUrl { get; set; }

        /// <summary>
        /// Gets the media platform settings.
        /// </summary>
        /// <value>The media platform settings.</value>
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        /// <summary>
        /// Gets or sets the aad application identifier.
        /// </summary>
        /// <value>The aad application identifier.</value>
        public string AadAppId { get; set; }

        /// <summary>
        /// Gets or sets the aad application secret.
        /// </summary>
        /// <value>The aad application secret.</value>
        public string AadAppSecret { get; set; }

        public string BotInternalHostingProtocol = "https";

        /// <summary>
        /// Gets or sets the instance internal port.
        /// </summary>
        /// <value>The instance internal port.</value>
        public int MediaInternalPort { get; set; }
        public int BotInternalPort { get; set; }

        /// <summary>
        /// Gets or sets the call signaling port.
        /// Internal port to listen for new calls load balanced
        /// from 443 => to this local port
        /// </summary>
        /// <value>The call signaling port.</value>
        public int BotCallingInternalPort { get; set; }

        public bool UseCognitiveServices { get; set; }
        public string SpeechConfigKey { get; set; }
        public string SpeechConfigRegion { get; set; }
        public string BotLanguage { get; set; }
        // set by dsc script
        public int BotInstanceExternalPort { get; set; }
        public int MediaInstanceExternalPort { get; set; }
        public bool UseLocalDevSettings { get; set; }

        public string MediaDnsName { get; set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (UseLocalDevSettings)
            {
                // if running locally with ngrok
                // the call signalling and notification will use the same internal and external ports
                // because you cannot receive requests on the same tunnel with different ports

                // calls come in over 443 (external) and route to the internally hosted port: BotCallingInternalPort
                BotInstanceExternalPort = 443;
                BotInternalPort = BotCallingInternalPort;
                BotInternalHostingProtocol = "http";

                if (string.IsNullOrEmpty(MediaDnsName)) throw new ArgumentNullException(nameof(MediaDnsName));
            }
            else
            {
                MediaDnsName = ServiceDnsName;
            }

            if (string.IsNullOrEmpty(ServiceDnsName)) throw new ArgumentNullException(nameof(ServiceDnsName));
            if (string.IsNullOrEmpty(CertificateThumbprint)) throw new ArgumentNullException(nameof(CertificateThumbprint));
            if (string.IsNullOrEmpty(AadAppId)) throw new ArgumentNullException(nameof(AadAppId));
            if (string.IsNullOrEmpty(AadAppSecret)) throw new ArgumentNullException(nameof(AadAppSecret));
            if (BotCallingInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotCallingInternalPort));
            if (BotInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotInstanceExternalPort));
            if (BotInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotInternalPort));
            if (MediaInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(MediaInstanceExternalPort));
            if (MediaInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(MediaInternalPort));

            ServiceCname = ServiceDnsName;
            Console.WriteLine("Fetching Certificate");
            X509Certificate2 defaultCertificate = this.GetCertificateFromStore();

            //List<string> controlListenUris = new List<string>();
            // localhost
            var baseDomain = "+";

            // externall URLs always are https
            var botCallingExternalUrl = $"https://{ServiceCname}:443/joinCall";
            var botCallingInternalUrl = $"{ BotInternalHostingProtocol }://localhost:{BotCallingInternalPort}/";

            var botInstanceExternalUrl = $"https://{ServiceCname}:{BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";
            var botInstanceInternalUrl = $"{BotInternalHostingProtocol}://localhost:{BotInternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";


            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri($"https://{this.ServiceCname}:{BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");

            // http for local development or where certificate is not installed
            // https for running on VM
            var controlListenUris = new HashSet<string>();
            controlListenUris.Add($"{BotInternalHostingProtocol}://{baseDomain}:{BotCallingInternalPort}/");
            controlListenUris.Add($"{BotInternalHostingProtocol}://{baseDomain}:{BotInternalPort}/");

            this.CallControlListeningUrls = controlListenUris;
            Console.WriteLine("Initializing Media");
            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = MediaInternalPort, // 8445
                    InstancePublicIPAddress = IPAddress.Any, // IPAddress.Parse("1.1.1.1"),
                    InstancePublicPort = MediaInstanceExternalPort, // 12332 // 6008
                    ServiceFqdn = MediaDnsName
                },
                ApplicationId = this.AadAppId,
            };

            Console.WriteLine("\n");
            Console.WriteLine($"-----EXTERNAL-----");
            Console.WriteLine($"Listening on: {botCallingExternalUrl} (New Incoming calls)");
            Console.WriteLine($"Listening on: {botInstanceExternalUrl} (Existing calls notifications/updates)");
            // media platform will ping this
            // [net.tcp://tcp.botlocal.<yourdomain>.com:12332/MediaProcessor]
            Console.WriteLine($"Listening on: net.tcp//{MediaDnsName}:{MediaInstanceExternalPort} (Media connection)");

            Console.WriteLine("\n");
            Console.WriteLine($"-----INTERNAL-----");
            Console.WriteLine($"Listening on: {botCallingInternalUrl} (New Incoming calls)");
            Console.WriteLine($"Listening on: {botInstanceInternalUrl} (Existing calls notifications/updates)");
            Console.WriteLine($"Listening on: net.tcp//localhost:{MediaInternalPort} (Media connection)");
        }

        /// <summary>
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <returns>Certificate if found.</returns>
        /// <exception cref="Exception">No certificate with thumbprint {CertificateThumbprint} was found in the machine store.</exception>
        private X509Certificate2 GetCertificateFromStore()
        {

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateThumbprint, validOnly: false);

                if (certs.Count != 1)
                {
                    throw new Exception($"No certificate with thumbprint {CertificateThumbprint} was found in the machine store.");
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }
    }

}
