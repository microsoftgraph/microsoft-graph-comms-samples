using System.ComponentModel.DataAnnotations;

namespace EchoBot
{
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the name of the service DNS.
        /// </summary>
        /// <value>The name of the service DNS.</value>
        [Required]
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
        [Required]
        public string AadAppId { get; set; }

        /// <summary>
        /// Gets or sets the aad application secret.
        /// </summary>
        /// <value>The aad application secret.</value>
        [Required]
        public string AadAppSecret { get; set; }

        ///// <summary>
        ///// Internal hosting protocol for the bot
        ///// With 'UseLocalDevSettings' this will be http
        ///// </summary>
        //public string BotInternalHostingProtocol = "http";

        /// <summary>
        /// Gets or sets the instance media internal port.
        /// </summary>
        /// <value>The instance internal port.</value>
        [Required]
        public int MediaInternalPort { get; set; }

        /// <summary>
        /// Gets or sets the instance bot notifications internal port
        /// </summary>
        [Required]
        public int BotInternalPort { get; set; }

        /// <summary>
        /// Gets or sets the call signaling port.
        /// Internal port to listen for new calls load balanced
        /// from 443 => to this local port
        /// </summary>
        /// <value>The call signaling port.</value>
        [Required]
        public int BotCallingInternalPort { get; set; }

        /// <summary>
        /// Gets or sets if the bot should use Cognitive Services
        /// for converting the audio to a Bot voice
        /// </summary>
        public bool UseCognitiveServices { get; set; }

        /// <summary>
        /// Gets or sets the Cognitive Services Speech key
        /// </summary>
        public string SpeechConfigKey { get; set; }

        /// <summary>
        /// Gets or sets the Cognitive Services Speech region
        /// </summary>
        public string SpeechConfigRegion { get; set; }

        /// <summary>
        /// Gets or sets the Cognitive Services Bot language
        /// that it will use for speech-to-text and text-to-speech
        /// </summary>
        public string BotLanguage { get; set; }

        // set by dsc script

        /// <summary>
        /// Gets or sets the Load Balancer port for the specific VM instance
        /// used for call notifications
        /// </summary>
        [Required]
        public int BotInstanceExternalPort { get; set; }

        /// <summary>
        /// Gets or sets the Load Balancer port for the specific VM instance
        /// used for media notifications
        /// </summary>
        [Required]
        public int MediaInstanceExternalPort { get; set; }

        /// <summary>
        /// Used for local development to set the ports to be used
        /// with ngrok
        /// </summary>
        public bool UseLocalDevSettings { get; set; }

        /// <summary>
        /// Set by the user only when using local dev settings
        /// since the media settings needs a different URI
        /// </summary>
        [Required]
        public string MediaDnsName { get; set; }
    }
}

