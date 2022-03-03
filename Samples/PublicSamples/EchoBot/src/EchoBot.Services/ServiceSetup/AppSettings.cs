namespace EchoBot.Services.ServiceSetup
{
    /// <summary>
    /// Class AppSettings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the name of the service DNS.
        /// </summary>
        /// <value>The name of the service DNS.</value>
        public string ServiceDnsName { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>The certificate thumbprint.</value>
        public string CertificateThumbprint { get; set; }

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
        public string AppInsightsInstrumentationKey { get; set; }
    }
}
