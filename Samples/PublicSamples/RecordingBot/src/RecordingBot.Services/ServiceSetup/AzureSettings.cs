using Microsoft.AspNetCore.Http;
using Microsoft.Skype.Bots.Media;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace RecordingBot.Services.ServiceSetup
{
    public class AzureSettings : IAzureSettings
    {
        public string ServiceDnsName { get; set; }
        public string ServicePath {get;set;} = "/";
        public string ServiceCname { get; set; }
        public string CertificateThumbprint { get; set; }
        public Uri CallControlBaseUrl { get; set; }
        public Uri PlaceCallEndpointUrl { get; set; }
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }
        public string AadAppId { get; set; }
        public string AadAppSecret { get; set; }
        public int InstancePublicPort { get; set; }
        public int InstanceInternalPort { get; set; }
        public int CallSignalingPort { get; set; }
        public int CallSignalingPublicPort {get;set;} = 443;
        public bool CaptureEvents { get; set; } = false;
        public string PodName { get; set; }
        public string MediaFolder { get; set; }
        public string EventsFolder { get; set; }
        public string TopicName { get; set; } = "recordingbotevents";
        public string RegionName { get; set; } = "australiaeast";
        public string TopicKey { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public bool IsStereo { get; set; }
        public int WAVSampleRate { get; set; }
        public int WAVQuality { get; set; }
        public PathString PodPathBase { get; private set; }
        public X509Certificate2 Certificate { get; private set; }

        public void Initialize()
        {
            if (string.IsNullOrWhiteSpace(ServiceCname))
            {
                ServiceCname = ServiceDnsName;
            }

            Certificate = GetCertificateFromStore();

            int podNumber = 0;

            if (!string.IsNullOrEmpty(PodName))
            {
                _ = int.TryParse(Regex.Match(PodName, @"\d+$").Value, out podNumber);
            }

            // Create structured config objects for service.
            CallControlBaseUrl = new Uri($"https://{ServiceCname}{(CallSignalingPublicPort != 443 ? ":" + CallSignalingPublicPort : "")}{ServicePath}{podNumber}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.OnNotificationRequestRoute}");
            PodPathBase = $"{ServicePath}{podNumber}";

            MediaPlatformSettings = new MediaPlatformSettings
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
                {
                    CertificateThumbprint = Certificate.Thumbprint,
                    InstanceInternalPort = InstanceInternalPort,
                    InstancePublicIPAddress = IPAddress.Any,
                    InstancePublicPort = InstancePublicPort + podNumber,
                    ServiceFqdn = ServiceCname
                },
                ApplicationId = AadAppId,
            };

            // Initialize Audio Settings
            AudioSettings = new AudioSettings
            {
                WavSettings = (WAVSampleRate > 0) ? new WAVSettings(WAVSampleRate, WAVQuality) : null
            };
        }

        /// <summary>
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <returns>Certificate if found.</returns>
        /// <exception cref="Exception">No certificate with thumbprint {CertificateThumbprint} was found in the machine store.</exception>
        private X509Certificate2 GetCertificateFromStore()
        {
            X509Store store = new(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateThumbprint, validOnly: false);

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
