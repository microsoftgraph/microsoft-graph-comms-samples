// ***********************************************************************
// Assembly         : EchoBot.Services
// Author           : JasonTheDeveloper
// Created          : 08-28-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="AzureSettings.cs" company="Microsoft Corporation">
//     Copyright ©  2020 Microsoft Corporation. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Skype.Bots.Media;
using EchoBot.Model.Constants;
using EchoBot.Services.Contract;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using EchoBot.Services.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EchoBot.Services.ServiceSetup
{
    /// <summary>
    /// Class AzureSettings.
    /// Implements the <see cref="EchoBot.Services.Contract.IAzureSettings" />
    /// </summary>
    /// <seealso cref="EchoBot.Services.Contract.IAzureSettings" />
    public class AzureSettings : IAzureSettings
    {
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

        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private readonly BotMediaLogger _mediaPlatformLogger;

        public AzureSettings(ILogger<AzureSettings> logger, IOptions<AppSettings> settings, IBotMediaLogger mediaLogger)
        {
            _logger = logger;
            _settings = settings.Value;
            //_mediaPlatformLogger = (MediaLogger)mediaLogger.Value;
            _mediaPlatformLogger = (BotMediaLogger)mediaLogger;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (_settings.UseLocalDevSettings)
            {
                // if running locally with ngrok
                // the call signalling and notification will use the same internal and external ports
                // because you cannot receive requests on the same tunnel with different ports

                // calls come in over 443 (external) and route to the internally hosted port: BotCallingInternalPort
                _settings.BotInstanceExternalPort = 443;
                _settings.BotInternalPort = _settings.BotCallingInternalPort;
                _settings.BotInternalHostingProtocol = "http";

                if (string.IsNullOrEmpty(_settings.MediaDnsName)) throw new ArgumentNullException(nameof(_settings.MediaDnsName));
            }
            else
            {
                _settings.MediaDnsName = _settings.ServiceDnsName;
            }

            if (string.IsNullOrEmpty(_settings.ServiceDnsName)) throw new ArgumentNullException(nameof(_settings.ServiceDnsName));
            if (string.IsNullOrEmpty(_settings.CertificateThumbprint)) throw new ArgumentNullException(nameof(_settings.CertificateThumbprint));
            if (string.IsNullOrEmpty(_settings.AadAppId)) throw new ArgumentNullException(nameof(_settings.AadAppId));
            if (string.IsNullOrEmpty(_settings.AadAppSecret)) throw new ArgumentNullException(nameof(_settings.AadAppSecret));
            if (_settings.BotCallingInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(_settings.BotCallingInternalPort));
            if (_settings.BotInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(_settings.BotInstanceExternalPort));
            if (_settings.BotInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(_settings.BotInternalPort));
            if (_settings.MediaInstanceExternalPort == 0) throw new ArgumentOutOfRangeException(nameof(_settings.MediaInstanceExternalPort));
            if (_settings.MediaInternalPort == 0) throw new ArgumentOutOfRangeException(nameof(_settings.MediaInternalPort));

            _logger.LogInformation("Fetching Certificate");
            X509Certificate2 defaultCertificate = this.GetCertificateFromStore();
            
            //List<string> controlListenUris = new List<string>();
            // localhost
            var baseDomain = "+";

            // external URLs always are https
            var botCallingExternalUrl = $"https://{_settings.ServiceDnsName}:443/joinCall";
            var botCallingInternalUrl = $"{ _settings.BotInternalHostingProtocol }://localhost:{_settings.BotCallingInternalPort}/";

            var botInstanceExternalUrl = $"https://{_settings.ServiceDnsName}:{_settings.BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";
            var botInstanceInternalUrl = $"{_settings.BotInternalHostingProtocol}://localhost:{_settings.BotInternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute} (Existing calls notifications/updates)";


            // Create structured config objects for service.
            CallControlBaseUrl = new Uri($"https://{_settings.ServiceDnsName}:{_settings.BotInstanceExternalPort}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");

            // http for local development
            // https for running on VM
            var controlListenUris = new HashSet<string>();
            controlListenUris.Add($"{_settings.BotInternalHostingProtocol}://{baseDomain}:{_settings.BotCallingInternalPort}/");
            controlListenUris.Add($"{_settings.BotInternalHostingProtocol}://{baseDomain}:{_settings.BotInternalPort}/");

            this.CallControlListeningUrls = controlListenUris;
            _logger.LogInformation("Initializing Media");
            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = _settings.MediaInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = _settings.MediaInstanceExternalPort,
                    ServiceFqdn = _settings.MediaDnsName
                },
                ApplicationId = _settings.AadAppId,
                MediaPlatformLogger = _mediaPlatformLogger
            };

            _logger.LogInformation($"-----EXTERNAL-----");
            _logger.LogInformation($"Listening on: {botCallingExternalUrl} (New Incoming calls)");
            _logger.LogInformation($"Listening on: {botInstanceExternalUrl} (Existing calls notifications/updates)");
            // media platform will ping this
            // [net.tcp://tcp.botlocal.<yourdomain>.com:12332/MediaProcessor]
            _logger.LogInformation($"Listening on: net.tcp//{_settings.MediaDnsName}:{_settings.MediaInstanceExternalPort} (Media connection)");

            _logger.LogInformation("\n");
            _logger.LogInformation($"-----INTERNAL-----");
            _logger.LogInformation($"Listening on: {botCallingInternalUrl} (New Incoming calls)");
            _logger.LogInformation($"Listening on: {botInstanceInternalUrl} (Existing calls notifications/updates)");
            _logger.LogInformation($"Listening on: net.tcp//localhost:{_settings.MediaInternalPort} (Media connection)");
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
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, _settings.CertificateThumbprint, validOnly: false);

                if (certs.Count != 1)
                {
                    throw new Exception($"No certificate with thumbprint {_settings.CertificateThumbprint} was found in the machine store.");
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
