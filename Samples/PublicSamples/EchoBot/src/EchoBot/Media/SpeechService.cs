using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Skype.Bots.Media;
using System.Runtime.InteropServices;

namespace EchoBot.Media
{
    public class SpeechService
    {
        private bool _isRunning = false;
        protected bool _isDraining;
        private readonly ILogger _logger;
        private readonly PushAudioInputStream _audioInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        private readonly AudioOutputStream _audioOutputStream = AudioOutputStream.CreatePullStream();
        private readonly SpeechConfig _speechConfig;
        private readonly SpeechSynthesizer _synthesizer;
        private TranslationRecognizer _translationRecognizer;

        // public event EventHandler<MediaStreamEventArgs> OnSendMediaBufferEventArgs;

        public SpeechService(AppSettings settings, ILogger logger)
        {
            _logger = logger;

            _speechConfig = SpeechConfig.FromSubscription(settings.SpeechConfigKey, settings.SpeechConfigRegion);
            _speechConfig.SpeechSynthesisLanguage = settings.BotLanguage;
            _speechConfig.SpeechRecognitionLanguage = settings.BotLanguage;

            var audioConfig = AudioConfig.FromStreamOutput(_audioOutputStream);
            _synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);
        }

        public async Task AppendAudioBuffer(AudioMediaBuffer audioBuffer)
        {
            if (!_isRunning)
            {
                Start();
                await ProcessSpeech();
            }

            try
            {
                var bufferLength = audioBuffer.Length;
                if (bufferLength > 0)
                {
                    var buffer = new byte[bufferLength];
                    Marshal.Copy(audioBuffer.Data, buffer, 0, (int)bufferLength);
                    _audioInputStream.Write(buffer);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened writing to input stream");
            }
        }

        public virtual void OnSendMediaBufferEventArgs(object sender, MediaStreamEventArgs e)
        {
            if (SendMediaBuffer != null)
            {
                SendMediaBuffer(this, e);
            }
        }

        public event EventHandler<MediaStreamEventArgs> SendMediaBuffer;

        /// <summary>
        /// Ends this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ShutDownAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            if (_isRunning)
            {
                await _translationRecognizer.StopContinuousRecognitionAsync();
                _translationRecognizer.Dispose();
                _audioInputStream.Close();

                _audioInputStream.Dispose();
                _audioOutputStream.Dispose();
                _synthesizer.Dispose();

                _isRunning = false;
            }
        }

        private void Start()
        {
            if (!_isRunning)
            {
                _isRunning = true;
            }
        }

        private async Task ProcessSpeech()
        {
            try
            {
                var stopRecognition = new TaskCompletionSource<int>();

                var translationConfig = SpeechTranslationConfig.FromSubscription(_speechConfig.SubscriptionKey, _speechConfig.Region);
                translationConfig.SpeechRecognitionLanguage = _speechConfig.SpeechRecognitionLanguage;
                translationConfig.AddTargetLanguage("vi");
                translationConfig.VoiceName = "vi-VN-HoaiMyNeural";

                using (var audioInput = AudioConfig.FromStreamInput(_audioInputStream))
                {
                    _translationRecognizer = new TranslationRecognizer(translationConfig, audioInput);
                }

                _translationRecognizer.Recognizing += (s, e) =>
                {
                    _logger.LogInformation($"RECOGNIZING: {e.Result.Text}");
                    foreach (var element in e.Result.Translations)
                    {
                        _logger.LogInformation($"TRANSLATING into '{element.Key}': {element.Value}");
                    }
                };

                _translationRecognizer.Recognized += async (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.TranslatedSpeech)
                    {
                        _logger.LogInformation($"RECOGNIZED: {e.Result.Text}");
                        foreach (var element in e.Result.Translations)
                        {
                            _logger.LogInformation($"TRANSLATING into '{element.Key}': {element.Value}");
                            await TextToSpeech(element.Value);
                        }
                    }
                };

                _translationRecognizer.Canceled += (s, e) =>
                {
                    _logger.LogInformation($"CANCELED: Reason={e.Reason}");
                    if (e.Reason == CancellationReason.Error)
                    {
                        _logger.LogInformation($"ErrorDetails={e.ErrorDetails}");
                    }
                    stopRecognition.TrySetResult(0);
                };

                _translationRecognizer.SessionStarted += (s, e) =>
                {
                    _logger.LogInformation("Session started.");
                };

                _translationRecognizer.SessionStopped += (s, e) =>
                {
                    _logger.LogInformation("Session stopped.");
                    stopRecognition.TrySetResult(0);
                };

                await _translationRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                Task.WaitAny(new[] { stopRecognition.Task });
                await _translationRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Caught Exception");
            }
            
            _isDraining = false;
        }

        private async Task TextToSpeech(string text)
        {
            SpeechSynthesisResult result = await _synthesizer.SpeakTextAsync(text);
            using (var stream = AudioDataStream.FromResult(result))
            {
                var currentTick = DateTime.Now.Ticks;
                MediaStreamEventArgs args = new MediaStreamEventArgs
                {
                    AudioMediaBuffers = Util.Utilities.CreateAudioMediaBuffers(stream, currentTick, _logger)
                };
                OnSendMediaBufferEventArgs(this, args);
            }
        }
    }
}
