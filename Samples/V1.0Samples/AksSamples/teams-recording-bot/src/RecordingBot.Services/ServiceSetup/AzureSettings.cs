// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 08-28-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-09-2020
// ***********************************************************************
// <copyright file="AzureSettings.cs" company="Microsoft Corporation">
//     Copyright ©  2020 Microsoft Corporation. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Skype.Bots.Media;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace RecordingBot.Services.ServiceSetup
{
    /// <summary>
    /// Class AzureSettings.
    /// Implements the <see cref="RecordingBot.Services.Contract.IAzureSettings" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Contract.IAzureSettings" />
    public class AzureSettings : IAzureSettings
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
        /// Gets or sets the place call endpoint URL.
        /// </summary>
        /// <value>The place call endpoint URL.</value>
        public Uri PlaceCallEndpointUrl { get; set; }

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
        /// Gets or sets the instance public port.
        /// </summary>
        /// <value>The instance public port.</value>
        public int InstancePublicPort { get; set; }

        /// <summary>
        /// Gets or sets the instance internal port.
        /// </summary>
        /// <value>The instance internal port.</value>
        public int InstanceInternalPort { get; set; }

        /// <summary>
        /// Gets or sets the call signaling port.
        /// </summary>
        /// <value>The call signaling port.</value>
        public int CallSignalingPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [capture events].
        /// </summary>
        /// <value><c>true</c> if [capture events]; otherwise, <c>false</c>.</value>
        public bool CaptureEvents { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the pod.
        /// </summary>
        /// <value>The name of the pod.</value>
        public string PodName { get; set; }

        /// <summary>
        /// Gets or sets the media folder.
        /// </summary>
        /// <value>The media folder.</value>
        public string MediaFolder { get; set; }

        /// <summary>
        /// Gets or sets the events folder.
        /// </summary>
        /// <value>The events folder.</value>
        public string EventsFolder { get; set; }

        // Event Grid Settings
        /// <summary>
        /// Gets or sets the name of the topic.
        /// </summary>
        /// <value>The name of the topic.</value>
        public string TopicName { get; set; } = "recordingbotevents";

        /// <summary>
        /// Gets or sets the name of the region.
        /// </summary>
        /// <value>The name of the region.</value>
        public string RegionName { get; set; } = "australiaeast";
        /// <summary>
        /// Gets or sets the topic key.
        /// </summary>
        /// <value>The topic key.</value>
        public string TopicKey { get; set; }

        /// <summary>
        /// Gets or sets the audio settings.
        /// </summary>
        /// <value>The audio settings.</value>
        public AudioSettings AudioSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is stereo.
        /// </summary>
        /// <value><c>true</c> if this instance is stereo; otherwise, <c>false</c>.</value>
        public bool IsStereo { get; set; }

        /// <summary>
        /// Gets or sets the wav sample rate.
        /// </summary>
        /// <value>The wav sample rate.</value>
        public int WAVSampleRate { get; set; }

        /// <summary>
        /// Gets or sets the wav quality.
        /// </summary>
        /// <value>The wav quality.</value>
        public int WAVQuality { get; set; }


        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (string.IsNullOrWhiteSpace(ServiceCname))
            {
                ServiceCname = ServiceDnsName;
            }

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore();

            List<string> controlListenUris = new List<string>();

            var baseDomain = "+";

            int podNumber = 0;

            if (!string.IsNullOrEmpty(this.PodName))
            {
                int.TryParse(Regex.Match(this.PodName, @"\d+$").Value, out podNumber);
            }

            // Create structured config objects for service.
            this.CallControlBaseUrl = new Uri($"https://{this.ServiceCname}/{podNumber}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");

            controlListenUris.Add($"https://{baseDomain}:{CallSignalingPort}/");
            controlListenUris.Add($"https://{baseDomain}:{CallSignalingPort}/{podNumber}/");
            controlListenUris.Add($"http://{baseDomain}:{CallSignalingPort + 1}/"); // required for AKS pod graceful termination

            this.CallControlListeningUrls = controlListenUris;

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = InstanceInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = InstancePublicPort + podNumber,
                    ServiceFqdn = this.ServiceCname
                },
                ApplicationId = this.AadAppId,
            };


            // Initialize Audio Settings
            this.AudioSettings = new AudioSettings
            {
                WavSettings = (WAVSampleRate > 0) ? new WAVSettings(WAVSampleRate, WAVQuality): null
            };
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
