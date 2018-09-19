﻿// <copyright file="AzureConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
namespace Sample.TestCallingBot.WorkerRole
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Azure;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Sample.Common.Logging;
    using Sample.TestCallingBot.FrontEnd;
    using Sample.TestCallingBot.FrontEnd.Http;

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
        /// The instance id token.
        /// Prefix of the InstanceId from the RoleEnvironment.
        /// </summary>
        private const string InstanceIdToken = "in_";

        /// <summary>
        /// localPort specified in <InputEndpoint name="DefaultCallControlEndpoint" protocol="tcp" port="443" localPort="9440" />
        /// in .csdef. This is needed for running in emulator. Currently only messaging can be debugged in the emulator.
        /// Media debugging in emulator will be supported in future releases.
        /// </summary>
        private const int DefaultPort = 9440;

        /// <summary>
        /// The Configuration.
        /// </summary>
        private static readonly AzureConfiguration Configuration = new AzureConfiguration();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        private AzureConfiguration()
        {
        }

        /// <summary>
        /// Gets the instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        public static AzureConfiguration Instance
        {
            get { return Configuration; }
        }

        /// <inheritdoc/>
        public string ServiceDnsName { get; private set; }

        /// <summary>
        /// Gets the service cname.
        /// </summary>
        public string ServiceCNAME { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<Uri> CallControlListeningUrls { get; private set; }

        /// <inheritdoc/>
        public Uri CallControlBaseUrl { get; private set; }

        /// <inheritdoc/>
        public Uri PlaceCallEndpointUrl { get; private set; }

        /// <inheritdoc/>
        public Uri AzureInstanceBaseUrl { get; private set; }

        /// <inheritdoc/>
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        /// <inheritdoc/>
        public string AadAppId { get; private set; }

        /// <inheritdoc/>
        public string AadAppSecret { get; private set; }

        /// <summary>
        /// Initialize from serviceConfig.
        /// </summary>
        public void Initialize()
        {
            // Collect config values from Azure config.
            TraceEndpointInfo();
            this.ServiceDnsName = GetString(ServiceDnsNameKey);
            this.ServiceCNAME = GetString(ServiceCNameKey, true);
            if (string.IsNullOrEmpty(this.ServiceCNAME))
            {
                this.ServiceCNAME = this.ServiceDnsName;
            }

            var placeCallEndpointUrlStr = GetString(PlaceCallEndpointUrlKey, true);
            if (!string.IsNullOrEmpty(placeCallEndpointUrlStr))
            {
                this.PlaceCallEndpointUrl = new Uri(placeCallEndpointUrlStr);
            }

            X509Certificate2 defaultCertificate = GetCertificateFromStore(DefaultCertificateKey);

            RoleInstanceEndpoint instanceCallControlEndpoint = RoleEnvironment.IsEmulated ? null : GetEndpoint(InstanceCallControlEndpointKey);
            RoleInstanceEndpoint defaultEndpoint = GetEndpoint(DefaultEndpointKey);
            RoleInstanceEndpoint mediaControlEndpoint = RoleEnvironment.IsEmulated ? null : GetEndpoint(InstanceMediaControlEndpointKey);

            int instanceCallControlInternalPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.IPEndpoint.Port;
            string instanceCallControlInternalIpAddress = RoleEnvironment.IsEmulated
                ? IPAddress.Loopback.ToString()
                : instanceCallControlEndpoint.IPEndpoint.Address.ToString();

            int instanceCallControlPublicPort = RoleEnvironment.IsEmulated ? DefaultPort : instanceCallControlEndpoint.PublicIPEndpoint.Port;
            int mediaInstanceInternalPort = RoleEnvironment.IsEmulated ? 8445 : mediaControlEndpoint.IPEndpoint.Port;
            int mediaInstancePublicPort = RoleEnvironment.IsEmulated ? 20100 : mediaControlEndpoint.PublicIPEndpoint.Port;

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

            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri(string.Format(
                "https://{0}:{1}/{2}/{3}",
                this.ServiceCNAME,
                instanceCallControlPublicPort,
                HttpRouteConstants.CallSignalingRoutePrefix,
                HttpRouteConstants.OnIncomingRequestRoute));

            this.AzureInstanceBaseUrl = new Uri(string.Format(
                "https://{0}:{1}/",
                this.ServiceCNAME,
                instanceCallControlPublicPort));

            TraceConfigValue("CallControlCallbackUri", this.CallControlBaseUrl);
            List<Uri> controlListenUris = new List<Uri>();

            if (RoleEnvironment.IsEmulated)
            {
                controlListenUris.Add(new Uri("http://" + defaultEndpoint.IPEndpoint.Address + ":" + DefaultPort + "/"));
            }
            else
            {
                controlListenUris.Add(new Uri("https://" + instanceCallControlIpEndpoint + "/"));
                controlListenUris.Add(new Uri("https://" + defaultEndpoint.IPEndpoint + "/"));
            }

            this.CallControlListeningUrls = controlListenUris;

            foreach (Uri uri in this.CallControlListeningUrls)
            {
                TraceConfigValue("Call control listening Uri", uri);
            }

            IPAddress publicInstanceIpAddress = RoleEnvironment.IsEmulated
                ? IPAddress.Loopback
                : GetInstancePublicIpAddress(this.ServiceDnsName);

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicIPAddress = publicInstanceIpAddress,
                    InstancePublicPort = mediaInstancePublicPort,
                    ServiceFqdn = this.ServiceCNAME,
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
        private static void TraceEndpointInfo()
        {
            string[] endpoints = RoleEnvironment.IsEmulated
                ? new string[] { DefaultEndpointKey }
                : new string[] { DefaultEndpointKey, InstanceMediaControlEndpointKey };

            foreach (string endpointName in endpoints)
            {
                RoleInstanceEndpoint endpoint = GetEndpoint(endpointName);
                StringBuilder info = new StringBuilder();
                info.AppendFormat("Internal=https://{0}, ", endpoint.IPEndpoint);
                string publicInfo = endpoint.PublicIPEndpoint == null ? "-" : endpoint.PublicIPEndpoint.Port.ToString();
                info.AppendFormat("PublicPort={0}", publicInfo);
                TraceConfigValue(endpointName, info);
            }
        }

        /// <summary>
        /// Write debug entries for the configuration.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="value">Configuration value.</param>
        private static void TraceConfigValue(string key, object value)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"{key} ->{value}");
        }

        /// <summary>
        /// Lookup endpoint by its name.
        /// </summary>
        /// <param name="name">Endpoint name.</param>
        /// <returns>Role instance endpoint.</returns>
        private static RoleInstanceEndpoint GetEndpoint(string name)
        {
            if (!RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(name, out RoleInstanceEndpoint endpoint))
            {
                throw new ConfigurationException(name, "No endpoint with name '{0}' was found.", name);
            }

            return endpoint;
        }

        /// <summary>
        /// Lookup configuration value.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="allowEmpty">If empty configurations are allowed.</param>
        /// <returns>Configuration value, if found.</returns>
        private static string GetString(string key, bool allowEmpty = false)
        {
            string s = CloudConfigurationManager.GetSetting(key);

            TraceConfigValue(key, s);

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
        private static List<string> GetStringList(string key)
        {
            return GetString(key).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <param name="key">Configuration key containing the Thumbprint to search.</param>
        /// <returns>Certificate if found.</returns>
        private static X509Certificate2 GetCertificateFromStore(string key)
        {
            string thumbprint = GetString(key);

            X509Certificate2 cert;

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (certs.Count != 1)
                {
                    throw new ConfigurationException(key, "No certificate with thumbprint {0} was found in the machine store.", thumbprint);
                }

                cert = certs[0];
            }
            finally
            {
                store.Close();
            }

            return cert;
        }

        /// <summary>
        /// Get the PIP for this instance.
        /// </summary>
        /// <param name="publicFqdn">DNS name of the service.</param>
        /// <returns>IPAddress.</returns>
        private static IPAddress GetInstancePublicIpAddress(string publicFqdn)
        {
            // get the instanceId for the current instance. It will be of the form  XXMediaBotRole_IN_0. Look for IN_ and then extract the number after it
            // Assumption: in_<instanceNumber> will the be the last in the instanceId
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            int instanceIdIndex = instanceId.IndexOf(InstanceIdToken, StringComparison.OrdinalIgnoreCase);
            if (!int.TryParse(instanceId.Substring(instanceIdIndex + InstanceIdToken.Length), out int instanceNumber))
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Couldn't extract Instance index from {0}", instanceId);
                throw new Exception("Couldn't extract Instance index from " + instanceId);
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