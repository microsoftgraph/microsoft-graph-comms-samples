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

    /// <summary>
    /// Reads the Configuration from service Configuration.
    /// </summary>
    internal class AzureConfiguration : IConfiguration
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
        /// localPort specified in <InputEndpoint name="DefaultCallControlEndpoint" protocol="tcp" port="443" localPort="9441" />
        /// in .csdef. This is needed for running in emulator. Currently only messaging can be debugged in the emulator.
        /// Media debugging in emulator will be supported in future releases.
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

        /// <inheritdoc/>
        public string ServiceDnsName { get; private set; }

        /// <summary>
        /// Gets the service cname.
        /// </summary>
        public string ServiceCname { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<Uri> CallControlListeningUrls { get; private set; }

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

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore(DefaultCertificateKey);

            RoleInstanceEndpoint instanceCallControlEndpoint = RoleEnvironment.IsEmulated ? null : this.GetEndpoint(InstanceCallControlEndpointKey);
            RoleInstanceEndpoint defaultEndpoint = this.GetEndpoint(DefaultEndpointKey);
            RoleInstanceEndpoint mediaControlEndpoint = RoleEnvironment.IsEmulated ? null : this.GetEndpoint(InstanceMediaControlEndpointKey);

            int instanceCallControlInternalPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.IPEndpoint.Port;
            string instanceCallControlInternalIpAddress = RoleEnvironment.IsEmulated
                ? IPAddress.Loopback.ToString()
                : instanceCallControlEndpoint.IPEndpoint.Address.ToString();

            int instanceCallControlPublicPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.PublicIPEndpoint.Port;
            int mediaInstanceInternalPort = RoleEnvironment.IsEmulated ? 8445 : mediaControlEndpoint.IPEndpoint.Port;
            int mediaInstancePublicPort = RoleEnvironment.IsEmulated ? 13016 : mediaControlEndpoint.PublicIPEndpoint.Port;

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

            List<Uri> controlListenUris = new List<Uri>();
            if (RoleEnvironment.IsEmulated)
            {
                // Create structured config objects for service.
                this.CallControlBaseUrl = new Uri(string.Format(
                    "https://{0}/{1}",
                    this.ServiceCname,
                    HttpRouteConstants.CallSignalingRoutePrefix));

                controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint.Address + ":" + DefaultPort + "/"));
                controlListenUris.Add(new Uri("http://" + defaultEndpoint.IPEndpoint.Address + ":" + (DefaultPort + 1) + "/"));
            }
            else
            {
                // Create structured config objects for service.
                this.CallControlBaseUrl = new Uri(string.Format(
                    "https://{0}:{1}/{2}",
                    this.ServiceCname,
                    instanceCallControlPublicPort,
                    HttpRouteConstants.CallSignalingRoutePrefix));

                controlListenUris.Add(new Uri("https://" + instanceCallControlIpEndpoint + "/"));
                controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint + "/"));
            }

            this.TraceConfigValue("CallControlCallbackUri", this.CallControlBaseUrl);
            this.CallControlListeningUrls = controlListenUris;

            foreach (Uri uri in this.CallControlListeningUrls)
            {
                this.TraceConfigValue("Call control listening Uri", uri);
            }

            IPAddress publicInstanceIpAddress = RoleEnvironment.IsEmulated
                ? IPAddress.Any
                : this.GetInstancePublicIpAddress(this.ServiceDnsName);

            string serviceFqdn = RoleEnvironment.IsEmulated ? "0.ngrok.skype-graph-test.net" : this.ServiceCname;

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
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <param name="key">Configuration key containing the Thumbprint to search.</param>
        /// <returns>Certificate if found.</returns>
        private X509Certificate2 GetCertificateFromStore(string key)
        {
            string thumbprint = this.GetString(key);

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
    }
}