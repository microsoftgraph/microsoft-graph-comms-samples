using Microsoft.Skype.Bots.Media;
using Sample.AudioVideoPlaybackBot.FrontEnd;
using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace AVPWindowsService
{
    /// <summary>
    /// Class AzureSettings.
    /// Implements the <see cref="EchoBot.Services.Contract.IAzureSettings" />
    /// </summary>
    /// <seealso cref="EchoBot.Services.Contract.IAzureSettings" />
    public class WindowsServiceConfiguration : IConfiguration
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
        public IEnumerable<Uri> CallControlListeningUrls { get; set; }

        /// <inheritdoc/>
        public Uri PlaceCallEndpointUrl { get; private set; }

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

        /// <summary>
        /// Gets the h264 1280 x 720 file location.
        /// </summary>
        public string H2641280X72030FpsFile { get; private set; }

        /// <summary>
        /// Gets the h264 640 x 360 file location.
        /// </summary>
        public string H264640X36030FpsFile { get; private set; }

        /// <summary>
        /// Gets the h264 320 x 180 file location.
        /// </summary>
        public string H264320X18015FpsFile { get; private set; }


        /// <inheritdoc/>
        public Dictionary<string, VideoFormat> H264FileLocations { get; private set; }

        /// <inheritdoc/>
        public string AudioFileLocation { get; private set; }

        /// <inheritdoc/>
        public int AudioVideoFileLengthInSec { get; private set; }

        /// <summary>
        /// Gets the h264 1920 x 1080 vbss file location.
        /// </summary>
        public string H2641920X108015VBSSFpsFile { get; private set; }

        public string BotInternalHostingProtocol = "https";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>

        private const string H2641280X72030FpsKey = "F:\\API\\AVB\\output720p.264";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H264640X36030FpsKey = "F:\\API\\AVB\\output360p.264";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H264320X18015FpsKey = "F:\\API\\AVB\\output180p.264";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H2641920X1080VBSS15FpsKey = "F:\\API\\AVB\\mle1080p15vbss_2500Kbps.264";

        private const string AudioFileLocationKey = "F:\\API\\AVB\\downsampled.wav";


        private const string AudioVideoFileLengthInSecKey = "70";

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
        // set by dsc script
        public int BotInstanceExternalPort { get; set; }
        public int MediaInstanceExternalPort { get; set; }
        public bool UseLocalDevSettings { get; set; }

        public string MediaDnsName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public WindowsServiceConfiguration(EnvironmentVarConfigs envConfigs)
        {
            if (!System.Diagnostics.EventLog.SourceExists(SampleConstants.EventLogSource))
            {
                EventLog.CreateEventSource(SampleConstants.EventLogSource, SampleConstants.EventLogType);
            }
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Initializing {nameof(WindowsServiceConfiguration)}", EventLogEntryType.Warning);

            this.MapEnvironmentVars(envConfigs);
            this.Initialize();
        }

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
                BotInternalHostingProtocol = "https";
                MediaDnsName = ServiceDnsName;
            }

            // Enivonment Var Validations 
            if (string.IsNullOrEmpty(ServiceDnsName)) throw new ArgumentNullException(nameof(ServiceDnsName));
            if (string.IsNullOrEmpty(CertificateThumbprint)) throw new ArgumentNullException(nameof(CertificateThumbprint));
            if (string.IsNullOrEmpty(AadAppId)) throw new ArgumentNullException(nameof(AadAppId));
            if (string.IsNullOrEmpty(AadAppSecret)) throw new ArgumentNullException(nameof(AadAppSecret));
            if (BotCallingInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotCallingInternalPort));
            if (BotInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotInstanceExternalPort));
            if (BotInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(BotInternalPort));
            if (MediaInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(MediaInstanceExternalPort));
            if (MediaInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(MediaInternalPort));

            EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration Cname {this.ServiceCname} DNS {this.ServiceDnsName}", EventLogEntryType.Warning);
            if (string.IsNullOrEmpty(this.ServiceCname))
            {
                this.ServiceCname = this.ServiceDnsName;
            }

            this.PlaceCallEndpointUrl = new Uri("https://graph.microsoft.com/v1.0");

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore();

            // localhost
            var baseDomain = "localhost";

            // externall URLs always are https
            var botCallingExternalUrl = $"https://{ServiceCname}:443/joinCall";
            var botCallingInternalUrl = $"{ BotInternalHostingProtocol }://localhost:{BotCallingInternalPort}/";

            var botInstanceExternalUrl = $"https://{ServiceCname}:{BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";
            var botInstanceInternalUrl = $"{BotInternalHostingProtocol}://localhost:{BotInternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";


            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri($"https://{this.ServiceCname}:{BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}");
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration CallControlBaseUrl {this.CallControlBaseUrl}", EventLogEntryType.Warning);

            // http for local development or where certificate is not installed
            // https for running on VM
            var controlListenUris = new HashSet<Uri>();
            if (UseLocalDevSettings)
            {
                //Force internal to http
                controlListenUris.Add(new Uri($"http://{baseDomain}:{BotCallingInternalPort}/"));
                EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration controlListenUrl 1 {$"http://{baseDomain}:{BotCallingInternalPort}/"}", EventLogEntryType.Warning);
                controlListenUris.Add(new Uri($"http://{baseDomain}:{BotInternalPort}/"));
                EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration controlListenUrl 2 {$"http://{baseDomain}:{BotInternalPort}/"}", EventLogEntryType.Warning);
            }
            else
            {
                 //Add DSN CName for external listening
                controlListenUris.Add(new Uri($"{BotInternalHostingProtocol}://{this.ServiceCname}:{BotCallingInternalPort}/"));
                EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration controlListenUrl 1 {$"{BotInternalHostingProtocol}://{this.ServiceCname}:{BotCallingInternalPort}/"}", EventLogEntryType.Warning);
                controlListenUris.Add(new Uri($"{BotInternalHostingProtocol}://{this.ServiceCname}:{BotInternalPort}/"));
                EventLog.WriteEntry(SampleConstants.EventLogSource, $"WindowsServiceConfiguration controlListenUrl 2 {$"{BotInternalHostingProtocol}://{this.ServiceCname}:{BotInternalPort}/"}", EventLogEntryType.Warning);
            }
            this.CallControlListeningUrls = controlListenUris;

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = MediaInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = MediaInstanceExternalPort,
                    ServiceFqdn = MediaDnsName
                },
                ApplicationId = this.AadAppId,
            };

            this.H2641280X72030FpsFile = H2641280X72030FpsKey;
            this.H264320X18015FpsFile = H264320X18015FpsKey;
            this.H264640X36030FpsFile = H264640X36030FpsKey;
            this.H2641920X108015VBSSFpsFile = H2641920X1080VBSS15FpsKey;
            if (string.IsNullOrEmpty(this.H2641280X72030FpsFile) ||
                string.IsNullOrEmpty(this.H264320X18015FpsFile) ||
                string.IsNullOrEmpty(this.H264640X36030FpsFile) ||
                string.IsNullOrEmpty(this.H2641920X108015VBSSFpsFile))
            {
                throw new ArgumentNullException("H264Files", "Update app.config in WorkerRole with all the h264 files with the specified resolutions");
            }

            this.H264FileLocations = new Dictionary<string, VideoFormat>();
            this.H264FileLocations.Add(this.H2641280X72030FpsFile, VideoFormat.H264_1280x720_30Fps);
            this.H264FileLocations.Add(this.H264320X18015FpsFile, VideoFormat.H264_320x180_15Fps);
            this.H264FileLocations.Add(this.H264640X36030FpsFile, VideoFormat.H264_640x360_30Fps);
            this.H264FileLocations.Add(this.H2641920X108015VBSSFpsFile, VideoFormat.H264_1920x1080_15Fps);

            this.AudioFileLocation = AudioFileLocationKey;
            if (string.IsNullOrEmpty(this.AudioFileLocation))
            {
                throw new ArgumentNullException("AudioFileLocation", "Update app.config in WorkerRole with the audio file location");
            }

            try
            {
                this.AudioVideoFileLengthInSec = int.Parse(AudioVideoFileLengthInSecKey);
            }
            catch (Exception)
            {
                throw new ArgumentNullException("AudioVideoFileLengthInSec", "Update app.config in WorkerRole with the audio len in secs");
            }
            

            EventLog.WriteEntry(SampleConstants.EventLogSource, $"-----EXTERNAL-----", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: {botCallingExternalUrl} (New Incoming calls)", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: {botInstanceExternalUrl} (Existing calls notifications/updates)", EventLogEntryType.Information);

            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: net.tcp//{MediaDnsName}:{MediaInstanceExternalPort} (Media connection)", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"-----INTERNAL-----", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: {botCallingInternalUrl} (Existing calls notifications/updates)", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: {botInstanceInternalUrl} (Existing calls notifications/updates)", EventLogEntryType.Information);
            EventLog.WriteEntry(SampleConstants.EventLogSource, $"Listening on: net.tcp//localhost:{MediaInternalPort} (Media connection)", EventLogEntryType.Information);

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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void MapEnvironmentVars(EnvironmentVarConfigs envs)
        {
            if (envs == null)
            { 
                throw new ArgumentNullException(nameof(envs));
            }
            this.AadAppId = envs.AadAppId;
            this.AadAppSecret = envs.AadAppSecret;
            this.BotName = envs.BotName;
            this.BotInternalPort = envs.BotInternalPort;
            this.BotInstanceExternalPort = envs.BotInstanceExternalPort;   
            this.BotCallingInternalPort = envs.BotCallingInternalPort;
            this.ServiceCname = envs.ServiceCname;
            this.ServiceDnsName = envs.ServiceDnsName;
            this.CertificateThumbprint = envs.CertificateThumbprint;
            this.MediaInternalPort = envs.MediaInternalPort;
            this.MediaInstanceExternalPort = envs.MediaInstanceExternalPort;
        }
    }

}
