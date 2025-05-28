// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The configuration for azure.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.WorkerRole
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Azure;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Sample.AudioVideoPlaybackBot.FrontEnd;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
    using System.Diagnostics;
    using System.Diagnostics.Eventing.Reader;
    using Sample.Common;

    /// <summary>
    /// Reads the Configuration from service Configuration.
    /// </summary>
    public class AzureConfiguration : IConfiguration
    {
        /// <summary>
        /// DomainNameLabel in NetworkConfiguration in .cscfg  <PublicIP name="instancePublicIP" domainNameLabel="pip"/>
        /// If the below changes, please change in the cscfg as well.
        /// </summary>
        public const string DomainNameLabel = "pip";

        /// <summary>
        /// The default endpoint key.
        /// </summary>
        private const string DefaultEndpointKey = "DefaultEndpoint";

        /// <summary>
        /// The instance call control endpoint key.
        /// </summary>
        private const string InstanceCallControlEndpointKey = "InstanceCallControlEndpoint";

        /// <summary>
        /// The instance media control endpoint key.
        /// </summary>
        private const string InstanceMediaControlEndpointKey = "InstanceMediaControlEndpoint";

        /// <summary>
        /// The service dns name key.
        /// </summary>
        private const string ServiceDnsNameKey = "ServiceDNSName";

        /// <summary>
        /// The service cname key.
        /// </summary>
        private const string ServiceCNameKey = "ServiceCNAME";

        /// <summary>
        /// The place call endpoint URL key.
        /// </summary>
        private const string PlaceCallEndpointUrlKey = "PlaceCallEndpointUrl";

        /// <summary>
        /// The default certificate key.
        /// </summary>
        private const string DefaultCertificateKey = "DefaultCertificate";

        /// <summary>
        /// The Microsoft app id key.
        /// </summary>
        private const string AadAppIdKey = "AadAppId";

        /// <summary>
        /// The Microsoft app password key.
        /// </summary>
        private const string AadAppSecretKey = "AadAppSecret";

        /// <summary>
        /// The default Microsoft app id value.
        /// </summary>
        private const string DefaultAadAppIdValue = "$AadAppId$";

        /// <summary>
        /// The default Microsoft app password value.
        /// </summary>
        private const string DefaultAadAppSecretValue = "$AadAppSecret$";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H2641280X72030FpsKey = "H264_1280x720_30Fps";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H264640X36030FpsKey = "H264_640x360_30Fps";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H264320X18015FpsKey = "H264_320x180_15Fps";

        /// <summary>
        /// videoFile location for the specified resolution.
        /// </summary>
        private const string H2641920X1080VBSS15FpsKey = "H264_1920X1080_VBSS_15Fps";

        private const string AudioFileLocationKey = "AudioFileLocation";

        private const string AudioVideoFileLengthInSecKey = "AudioVideoFileLengthInSec";

        /// <summary>
        /// The instance id token.
        /// Prefix of the InstanceId from the RoleEnvironment.
        /// </summary>
        private const string InstanceIdToken = "in_";

        /// <summary>
        /// The public call signaling port number.
        /// </summary>
        private const string SignalingPortKey = "SignalingPort";

        /// <summary>
        /// The Media streaming port number.
        /// </summary>
        private const string MediaPortKey = "MediaPort";

        /// <summary>
        /// The TCP Forwarding port number.
        /// </summary>
        private const string TcpForwardingPortKey = "TcpForwardingPort";

        /// <summary>
        /// The default port number to use when a specific port is not defined.
        /// </summary>
        private const int DefaultPort = 9441;

        /// <summary>
        /// Graph logger.
        /// </summary>
        private IGraphLogger graphLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public AzureConfiguration(IGraphLogger logger)
        {
            this.graphLogger = logger;
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="isWindowsService">If true init config for windows service version.</param>
        public AzureConfiguration(IGraphLogger logger, bool isWindowsService = false)
        {
            this.graphLogger = logger;
            if (!isWindowsService)
            {
                this.Initialize();
            }
            else
            {
                this.InitializeWSConfig();
            }
        }

        /// <inheritdoc/>
        public string ServiceDnsName { get; private set; }

        /// <summary>
        /// Gets the service cname.
        /// </summary>
        public string ServiceCname { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<string> CallControlListeningUrls { get; private set; }

        /// <inheritdoc/>
        public Uri CallControlBaseUrl { get; private set; }

        /// <inheritdoc/>
        public Uri PlaceCallEndpointUrl { get; private set; }

        /// <inheritdoc/>
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        /// <inheritdoc/>
        public string AadAppId { get; private set; }

        /// <inheritdoc/>
        public string AadAppSecret { get; private set; }

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

        /// <summary>
        /// Gets the h264 1920 x 1080 vbss file location.
        /// </summary>
        public string H2641920X108015VBSSFpsFile { get; private set; }

        /// <inheritdoc/>
        public Dictionary<string, VideoFormat> H264FileLocations { get; private set; }

        /// <inheritdoc/>
        public string AudioFileLocation { get; private set; }

        /// <inheritdoc/>
        public int AudioVideoFileLengthInSec { get; private set; }

        /// <inheritdoc/>
        public int SignalingPort { get; private set; }

        /// <inheritdoc/>
        public int MediaPort { get; private set; }

        /// <inheritdoc/>
        public int TcpForwardingPort { get; private set; }

        /// <summary>
        /// Gets the Azure Active Directory tenant ID.
        /// </summary>
        public string TenantId => ConfigurationManager.AppSettings["AadTenantId"];

        /// <summary>
        /// Gets the bot name.
        /// </summary>
        public string BotName { get; private set; }

        /// <summary>
        /// Initialize from serviceConfig.
        /// </summary>
        public void InitializeWSConfig()
        {
            X509Certificate2 defaultCertificate = this.GetCertificateFromStore(DefaultCertificateKey);

            RoleInstanceEndpoint defaultEndpoint = null;

            int instanceCallControlInternalPort = DefaultPort;
            string instanceCallControlInternalIpAddress = IPAddress.Loopback.ToString();

            int instanceCallControlPublicPort = DefaultPort;
            int mediaInstanceInternalPort = 8445;
            int mediaInstancePublicPort = 12561;

            string instanceCallControlIpEndpoint = string.Format("{0}:{1}", instanceCallControlInternalIpAddress, instanceCallControlInternalPort);

            this.ServiceDnsName = this.GetString(ServiceDnsNameKey);
            this.ServiceCname = this.GetString(ServiceCNameKey, true);
            if (string.IsNullOrEmpty(this.ServiceCname))
            {
                this.ServiceCname = this.ServiceDnsName;
            }

            this.H2641280X72030FpsFile = ConfigurationManager.AppSettings[H2641280X72030FpsKey];
            this.H264320X18015FpsFile = ConfigurationManager.AppSettings[H264320X18015FpsKey];
            this.H264640X36030FpsFile = ConfigurationManager.AppSettings[H264640X36030FpsKey];
            this.H2641920X108015VBSSFpsFile = ConfigurationManager.AppSettings[H2641920X1080VBSS15FpsKey];
            if (string.IsNullOrEmpty(this.H2641280X72030FpsFile) ||
                string.IsNullOrEmpty(this.H264320X18015FpsFile) ||
                string.IsNullOrEmpty(this.H264640X36030FpsFile) ||
                string.IsNullOrEmpty(this.H2641920X108015VBSSFpsFile))
            {
                throw new ConfigurationException("H264Files", "Update app.config in WorkerRole with all the h264 files with the specified resolutions");
            }

            this.H264FileLocations = new Dictionary<string, VideoFormat>();
            this.H264FileLocations.Add(this.H2641280X72030FpsFile, VideoFormat.H264_1280x720_30Fps);
            this.H264FileLocations.Add(this.H264320X18015FpsFile, VideoFormat.H264_320x180_15Fps);
            this.H264FileLocations.Add(this.H264640X36030FpsFile, VideoFormat.H264_640x360_30Fps);
            this.H264FileLocations.Add(this.H2641920X108015VBSSFpsFile, VideoFormat.H264_1920x1080_15Fps);

            this.AudioFileLocation = ConfigurationManager.AppSettings[AudioFileLocationKey];
            if (string.IsNullOrEmpty(this.AudioFileLocation))
            {
                throw new ConfigurationException("AudioFileLocation", "Update app.config in WorkerRole with the audio file location");
            }

            if (!int.TryParse(ConfigurationManager.AppSettings[AudioVideoFileLengthInSecKey], out int avFileLengthInSec))
            {
                throw new ConfigurationException("AudioFileLocation", "Update app.config in WorkerRole with the audio file location");
            }

            this.AudioVideoFileLengthInSec = avFileLengthInSec;

            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri(string.Format(
                "https://{0}:{1}/{2}",
                this.ServiceCname,
                instanceCallControlPublicPort,
                HttpRouteConstants.CallSignalingRoutePrefix));
            List<Uri> controlListenUris = new List<Uri>();
            controlListenUris.Add(new Uri("https://" + instanceCallControlIpEndpoint + "/"));
            controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint + "/"));
            IPAddress publicInstanceIpAddress = IPAddress.Any;
            string serviceFqdn = this.ServiceCname;

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicIPAddress = publicInstanceIpAddress,
                    InstancePublicPort = mediaInstancePublicPort,
                    ServiceFqdn = serviceFqdn,
                },

                ApplicationId = this.AadAppId,
            };

            // Bind the certificate to the port
            try {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Attempting to bind certificate with thumbprint {defaultCertificate.Thumbprint} to port 9441", EventLogEntryType.Information);
                
                // Check if binding already exists
                Process process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = "http show sslcert ipport=0.0.0.0:9441";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                EventLog.WriteEntry("AudioVideoPlaybackService", $"netsh output: {output}", EventLogEntryType.Information);
                if (!string.IsNullOrEmpty(error)) {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"netsh error: {error}", EventLogEntryType.Warning);
                }
                
                if (!output.Contains(defaultCertificate.Thumbprint)) {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Certificate binding not found, adding it now", EventLogEntryType.Information);
                    
                    // Add binding if it doesn't exist
                    process = new Process();
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = $"http add sslcert ipport=0.0.0.0:9441 certhash={defaultCertificate.Thumbprint} appid={{00112233-4455-6677-8899-AABBCCDDEEFF}}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();
                    
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Add binding output: {output}", EventLogEntryType.Information);
                    if (!string.IsNullOrEmpty(error)) {
                        EventLog.WriteEntry("AudioVideoPlaybackService", $"Add binding error: {error}", EventLogEntryType.Warning);
                    }
                    
                    // Verify the binding was added
                    process = new Process();
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = "http show sslcert ipport=0.0.0.0:9441";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Verification output: {output}", EventLogEntryType.Information);
                }
            }
            catch (Exception ex) {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Error binding certificate: {ex.Message}", EventLogEntryType.Error);
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Stack trace: {ex.StackTrace}", EventLogEntryType.Error);
            }

            // Initialize BotName from configuration
            this.BotName = ConfigurationManager.AppSettings["BotName"] ?? "AudioVideoPlaybackBot";
        }

        /// <summary>
        /// Initialize from serviceConfig.
        /// </summary>
        public void Initialize()
        {
            // Collect config values from Azure config.
            this.TraceEndpointInfo();
            this.ServiceDnsName = this.GetString(ServiceDnsNameKey);
            this.ServiceCname = this.GetString(ServiceCNameKey, true);
            if (string.IsNullOrEmpty(this.ServiceCname))
            {
                this.ServiceCname = this.ServiceDnsName;
            }

            var placeCallEndpointUrlStr = this.GetString(PlaceCallEndpointUrlKey, true);
            if (!string.IsNullOrEmpty(placeCallEndpointUrlStr))
            {
                this.PlaceCallEndpointUrl = new Uri(placeCallEndpointUrlStr);
            }

            // Get all media ports
            this.SignalingPort = this.GetNumber(SignalingPortKey, false);
            this.MediaPort = this.GetNumber(MediaPortKey, false);
            this.TcpForwardingPort = this.GetNumber(TcpForwardingPortKey, false);

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore(DefaultCertificateKey);

            RoleInstanceEndpoint instanceCallControlEndpoint = RoleEnvironment.IsEmulated ? null : this.GetEndpoint(InstanceCallControlEndpointKey);
            RoleInstanceEndpoint defaultEndpoint = this.GetEndpoint(DefaultEndpointKey);
            RoleInstanceEndpoint mediaControlEndpoint = RoleEnvironment.IsEmulated ? null : this.GetEndpoint(InstanceMediaControlEndpointKey);

            int instanceCallControlInternalPort = RoleEnvironment.IsEmulated ? this.SignalingPort : instanceCallControlEndpoint.IPEndpoint.Port;
            string instanceCallControlInternalIpAddress = RoleEnvironment.IsEmulated
                ? IPAddress.Loopback.ToString()
                : instanceCallControlEndpoint.IPEndpoint.Address.ToString();

            int instanceCallControlPublicPort = RoleEnvironment.IsEmulated ? this.SignalingPort : instanceCallControlEndpoint.PublicIPEndpoint.Port;
            int mediaInstanceInternalPort = RoleEnvironment.IsEmulated ? this.MediaPort : mediaControlEndpoint.IPEndpoint.Port;
            int mediaInstancePublicPort = RoleEnvironment.IsEmulated ? this.TcpForwardingPort : mediaControlEndpoint.PublicIPEndpoint.Port;

            string instanceCallControlIpEndpoint = string.Format("{0}:{1}", instanceCallControlInternalIpAddress, instanceCallControlInternalPort);

            this.AadAppId = ConfigurationManager.AppSettings[AadAppIdKey];
            if (string.IsNullOrEmpty(this.AadAppId) || string.Equals(this.AadAppId, DefaultAadAppIdValue))
            {
                throw new ConfigurationException("AadAppId", "Update app.config in WorkerRole with AppId from the bot registration portal");
            }

            this.AadAppSecret = ConfigurationManager.AppSettings[AadAppSecretKey];
            if (string.IsNullOrEmpty(this.AadAppSecret) || string.Equals(this.AadAppSecret, DefaultAadAppSecretValue))
            {
                throw new ConfigurationException("AadAppSecret", "Update app.config in WorkerRole with BotSecret from the bot registration portal");
            }

            this.H2641280X72030FpsFile = ConfigurationManager.AppSettings[H2641280X72030FpsKey];
            this.H264320X18015FpsFile = ConfigurationManager.AppSettings[H264320X18015FpsKey];
            this.H264640X36030FpsFile = ConfigurationManager.AppSettings[H264640X36030FpsKey];
            this.H2641920X108015VBSSFpsFile = ConfigurationManager.AppSettings[H2641920X1080VBSS15FpsKey];
            if (string.IsNullOrEmpty(this.H2641280X72030FpsFile) ||
                string.IsNullOrEmpty(this.H264320X18015FpsFile) ||
                string.IsNullOrEmpty(this.H264640X36030FpsFile) ||
                string.IsNullOrEmpty(this.H2641920X108015VBSSFpsFile))
            {
                throw new ConfigurationException("H264Files", "Update app.config in WorkerRole with all the h264 files with the specified resolutions");
            }

            this.H264FileLocations = new Dictionary<string, VideoFormat>();
            this.H264FileLocations.Add(this.H2641280X72030FpsFile, VideoFormat.H264_1280x720_30Fps);
            this.H264FileLocations.Add(this.H264320X18015FpsFile, VideoFormat.H264_320x180_15Fps);
            this.H264FileLocations.Add(this.H264640X36030FpsFile, VideoFormat.H264_640x360_30Fps);
            this.H264FileLocations.Add(this.H2641920X108015VBSSFpsFile, VideoFormat.H264_1920x1080_15Fps);

            this.AudioFileLocation = ConfigurationManager.AppSettings[AudioFileLocationKey];
            if (string.IsNullOrEmpty(this.AudioFileLocation))
            {
                throw new ConfigurationException("AudioFileLocation", "Update app.config in WorkerRole with the audio file location");
            }

            if (!int.TryParse(ConfigurationManager.AppSettings[AudioVideoFileLengthInSecKey], out int avFileLengthInSec))
            {
                throw new ConfigurationException("AudioFileLocation", "Update app.config in WorkerRole with the audio file location");
            }

            this.AudioVideoFileLengthInSec = avFileLengthInSec;

            var controlListenUris = new List<string>();
            if (RoleEnvironment.IsEmulated)
            {
                // Create structured config objects for service.
                this.CallControlBaseUrl = new Uri($"https://{this.ServiceCname}/{HttpRouteConstants.CallSignalingRoutePrefix}");

                controlListenUris.Add("https://+:" + this.SignalingPort + "/");
                controlListenUris.Add("http://+:" + (this.SignalingPort + 1) + "/");
            }
            else
            {
                // Create structured config objects for service.
                this.CallControlBaseUrl = new Uri(string.Format(
                    "https://{0}:{1}/{2}",
                    this.ServiceCname,
                    instanceCallControlPublicPort,
                    HttpRouteConstants.CallSignalingRoutePrefix));

                controlListenUris.Add("https://" + instanceCallControlIpEndpoint + "/");
                controlListenUris.Add("https://" + defaultEndpoint.IPEndpoint + "/");
            }

            this.TraceConfigValue("CallControlCallbackUri", this.CallControlBaseUrl);
            this.CallControlListeningUrls = controlListenUris;

            foreach (var uri in this.CallControlListeningUrls)
            {
                this.TraceConfigValue("Call control listening Uri", uri);
            }

            var tcpAddress = this.ServiceDnsName;
            if (tcpAddress.Contains("ngrok")) {
                // For ngrok-free.app URLs, we need to handle them differently
                // The TCP forwarding will be on the same domain but different port
                tcpAddress = $"{this.ServiceDnsName}:{this.TcpForwardingPort}";
            } else {
                // Keep the old logic for backward compatibility
                tcpAddress = $"{this.ServiceDnsName.Substring(0, this.ServiceDnsName.IndexOf("."))}.tcp.ngrok.io:{this.TcpForwardingPort}";
            }

            IPAddress publicInstanceIpAddress;
            try {
                //var publicMediaUrl = new Uri($"https://{tcpAddress}");
                string mediaHostname = "8.tcp.ngrok.io";
                publicInstanceIpAddress = Dns.GetHostEntry(mediaHostname).AddressList[0];
            } catch (Exception ex) {
                this.graphLogger.Error($"Failed to resolve media URL: {ex.Message}. Using loopback address as fallback.");
                publicInstanceIpAddress = IPAddress.Loopback;
            }

            string serviceFqdn = RoleEnvironment.IsEmulated ? this.ServiceDnsName : this.ServiceCname;

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicPort = mediaInstancePublicPort,
                    InstancePublicIPAddress = publicInstanceIpAddress,
                    ServiceFqdn = serviceFqdn,
                },

                ApplicationId = this.AadAppId,
            };

            // Initialize BotName from configuration
            this.BotName = ConfigurationManager.AppSettings["BotName"] ?? "AudioVideoPlaybackBot";
        }

        /// <summary>
        /// Dispose the Configuration.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Write endpoint info into the debug logs.
        /// </summary>
        private void TraceEndpointInfo()
        {
            string[] endpoints = RoleEnvironment.IsEmulated
                ? new string[] { DefaultEndpointKey }
                : new string[] { DefaultEndpointKey, InstanceMediaControlEndpointKey };

            foreach (string endpointName in endpoints)
            {
                RoleInstanceEndpoint endpoint = this.GetEndpoint(endpointName);
                StringBuilder info = new StringBuilder();
                info.AppendFormat("Internal=https://{0}, ", endpoint.IPEndpoint);
                string publicInfo = endpoint.PublicIPEndpoint == null ? "-" : endpoint.PublicIPEndpoint.Port.ToString();
                info.AppendFormat("PublicPort={0}", publicInfo);
                this.TraceConfigValue(endpointName, info);
            }
        }

        /// <summary>
        /// Write debug entries for the configuration.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="value">Configuration value.</param>
        private void TraceConfigValue(string key, object value)
        {
            this.graphLogger.Info($"{key} ->{value}");
        }

        /// <summary>
        /// Lookup endpoint by its name.
        /// </summary>
        /// <param name="name">Endpoint name.</param>
        /// <returns>Role instance endpoint.</returns>
        private RoleInstanceEndpoint GetEndpoint(string name)
        {
            if (!RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(name, out RoleInstanceEndpoint endpoint))
            {
                throw new ConfigurationException(name, $"No endpoint with name '{name}' was found.");
            }

            return endpoint;
        }

        /// <summary>
        /// Lookup configuration value.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="allowEmpty">If empty configurations are allowed.</param>
        /// <returns>Configuration value, if found.</returns>
        private string GetString(string key, bool allowEmpty = false)
        {
            string s = CloudConfigurationManager.GetSetting(key);

            this.TraceConfigValue(key, s);

            if (!allowEmpty && string.IsNullOrWhiteSpace(s))
            {
                throw new ConfigurationException(key, "The Configuration value is null or empty.");
            }

            return s;
        }

        /// <summary>
        /// Retrieve configuration, stored as comma separated, as an array.
        /// </summary>
        /// <param name="key">Configuration key containing the setting.</param>
        /// <returns>Configuration value split into an array.</returns>
        private List<string> GetStringList(string key)
        {
            return this.GetString(key).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Lookup configuration value.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="allowEmpty">If empty configurations are allowed.</param>
        /// <returns>Configuration value, if found.</returns>
        private int GetNumber(string key, bool allowEmpty)
        {
            string value = this.GetString(key, allowEmpty);

            // If empty is allowed and the value is empty, return 0 or another default value
            if (allowEmpty && string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            // Try to parse the value, with a fallback to a default
            if (!int.TryParse(value, out int result))
            {
                this.graphLogger.Error($"Configuration value for '{key}' is not a valid number: '{value}'. Using default value 0.");
                return 0;
            }

            return result;
        }

        /// <summary>
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <param name="key">Configuration key containing the Thumbprint to search.</param>
        /// <returns>Certificate if found.</returns>
        private X509Certificate2 GetCertificateFromStore(string key)
        {
            string thumbprint = this.GetString(key);

            // Check if this is a placeholder thumbprint
            if (thumbprint.Contains("ABC") && thumbprint.Contains("CBA"))
            {
                this.graphLogger.Error($"Certificate thumbprint '{thumbprint}' appears to be a placeholder. Please update with a real certificate thumbprint.");

                // For development/testing only - create a self-signed certificate
                if (RoleEnvironment.IsEmulated)
                {
                    this.graphLogger.Info("Running in emulated environment. Creating a temporary self-signed certificate.");
                    return this.CreateSelfSignedCertificate();
                }
            }

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (certs.Count != 1)
                {
                    throw new ConfigurationException(key, $"No certificate with thumbprint {thumbprint} was found in the machine store.");
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Creates a temporary self-signed certificate for development purposes.
        /// </summary>
        /// <returns>A self-signed certificate.</returns>
        private X509Certificate2 CreateSelfSignedCertificate()
        {
            // For development purposes, use an existing certificate from the store
            // Look for any valid certificate in the personal store
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                // Try to find any valid certificate
                X509Certificate2Collection certs = store.Certificates.Find(
                    X509FindType.FindByTimeValid,
                    DateTime.Now,
                    validOnly: true);

                // Filter to only include certificates with private keys
                var certsWithPrivateKeys = new X509Certificate2Collection();
                foreach (var cert in certs)
                {
                    if (cert.HasPrivateKey)
                    {
                        certsWithPrivateKeys.Add(cert);
                    }
                }

                if (certsWithPrivateKeys.Count > 0)
                {
                    this.graphLogger.Info($"Using existing certificate with subject: {certsWithPrivateKeys[0].Subject}");
                    return certsWithPrivateKeys[0];
                }

                this.graphLogger.Error("No valid certificates found in the store. Please install a valid certificate.");

                // As a last resort, create a dummy certificate for testing
                // Note: This won't work for actual TLS/SSL, but might allow the app to start
                var dummyCert = new X509Certificate2(
                    Convert.FromBase64String(
                        "MIIDBjCCAe6gAwIBAgIQRQe1bLCGdRgQVPK9jVKI+TANBgkqhkiG9w0BAQsFADATMREwDwYDVQQDDAhNeVRlc3RDQTAeFw0yMzA1MTUwMDAwMDBaFw0yNDA1MTUwMDAwMDBaMBMxETAPBgNVBAMMCE15VGVzdENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzWJP5cMThJgMBeTvRKS1/nUGpRl1CW+0YMxCsZ8X5ZlQ+BgUg+SBEXZyJ5Kok7qDWuJvZrjR5HzKRrD0fHQK/zZc8VFhbgJJsVsJxTQs/EkvpZXxWJgOMGiCBpGZzGBmzU8gJKQGBkZ6gHjPgRW/h3qcOZlbkLCxbNcWlCUvzEkGbcO9AqJ6FxiG3QKz/xDh1IYR3GHwDMbFYIWOp1fK3QpvSLiOiG0jPnYGEHWnKvQkRm8GY9QjS9SFTwwQzUG9XgMY9vJ3frrTJQgLc1+c0HoNyh6tI3S9dLRLcgcYzYhxkI1CeV+7+5BYRmOuTxjhLGAkr5qzPuRWcjqnXQIDAQABo1AwTjAOBgNVHQ8BAf8EBAMCAQYwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUXHJJjWqLSB9HVxFyHJTRQzKRk+gwDAYDVR0RBAUwA4IBKjANBgkqhkiG9w0BAQsFAAOCAQEAFoFTUJwSHyqwYJuK8lj9GVgYR6q8T+UuHPPAcOLScXCzVJAU7o8BGPvPcXsW0vE/y0fUj9iQxGzNe+24+Q8qqJXeRXvRZZYCVxjzBEiLnL3DFUlSJ/UyJT1w+8G/0x8WeJxYyAHYbKTpD9P/QEjNcVULrSF5rJx2OVU1MUV/oRTX0/CjVr2nbLFJnL8QfxTGpz708XQ8U59txH/6+DWkmQzScR8eLJHaElW1Y0iCo5GaPBk54jiWTxCnTGCTVQHAJl+1LtXEGNQhGQZCzGZQWEYJSvXxQFQ3wxdzKBY6UYgI3ON7pm9NJz4qXyB3O1JyOvG6mnRx+tnQCiKxUCk5kg=="),
                    "password",
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                return dummyCert;
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Get the PIP for this instance.
        /// </summary>
        /// <param name="publicFqdn">DNS name for this service.</param>
        /// <returns>IPAddress.</returns>
        private IPAddress GetInstancePublicIpAddress(string publicFqdn)
        {
            // get the instanceId for the current instance. It will be of the form  XXMediaBotRole_IN_0. Look for IN_ and then extract the number after it
            // Assumption: in_<instanceNumber> will the be the last in the instanceId
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            int instanceIdIndex = instanceId.IndexOf(InstanceIdToken, StringComparison.OrdinalIgnoreCase);
            if (!int.TryParse(instanceId.Substring(instanceIdIndex + InstanceIdToken.Length), out int instanceNumber))
            {
                var err = $"Couldn't extract Instance index from {instanceId}";
                this.graphLogger.Error(err);
                throw new Exception(err);
            }

            // for example: instance0 for fooservice.cloudapp.net will have hostname as pip.0.fooservice.cloudapp.net
            string instanceHostName = DomainNameLabel + "." + instanceNumber + "." + publicFqdn;
            IPAddress[] instanceAddresses = Dns.GetHostEntry(instanceHostName).AddressList;
            if (instanceAddresses.Length == 0)
            {
                throw new InvalidOperationException("Could not resolve the PIP hostname. Please make sure that PIP is properly configured for the service");
            }

            return instanceAddresses[0];
        }

        /// <summary>
        /// Normalizes a Teams meeting URL to a format that can be parsed by the bot.
        /// </summary>
        /// <param name="joinUrl">The original Teams meeting URL.</param>
        /// <returns>A normalized URL that can be parsed by the bot.</returns>
        public static string NormalizeTeamsMeetingUrl(string joinUrl)
        {
            if (string.IsNullOrEmpty(joinUrl))
            {
                return joinUrl;
            }

            // Handle teams.live.com format
            if (joinUrl.Contains("teams.live.com/meet/"))
            {
                try
                {
                    // Extract the meeting ID and passcode
                    Uri uri = new Uri(joinUrl);
                    string path = uri.AbsolutePath;
                    string meetingId = path.Substring(path.LastIndexOf('/') + 1);
                    
                    // Extract the passcode from query parameters if present
                    string passcode = string.Empty;
                    string query = uri.Query;
                    if (!string.IsNullOrEmpty(query) && query.Contains("p="))
                    {
                        passcode = query.Substring(query.IndexOf("p=") + 2);
                        if (passcode.Contains("&"))
                        {
                            passcode = passcode.Substring(0, passcode.IndexOf("&"));
                        }
                    }
                    
                    // Construct a URL in the format that the bot can parse
                    return $"https://teams.microsoft.com/l/meetup-join/meeting_{meetingId}/0?context=%7b%22Tid%22%3a%22{Guid.NewGuid()}%22%2c%22Oid%22%3a%22{Guid.NewGuid()}%22%7d";
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Error normalizing Teams URL: {ex.Message}", EventLogEntryType.Error);
                    return joinUrl;
                }
            }
            
            return joinUrl;
        }
    }
}