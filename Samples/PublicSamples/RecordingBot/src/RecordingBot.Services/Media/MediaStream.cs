using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using RecordingBot.Model.Constants;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RecordingBot.Services.Media
{
    public class MediaStream : IMediaStream
    {
        private readonly AzureSettings _settings;
        private readonly IGraphLogger _logger;
        private readonly string _mediaId;

        private readonly BufferBlock<SerializableAudioMediaBuffer> _buffer;
        private readonly CancellationTokenSource _tokenSource;

        private readonly AudioProcessor _currentAudioProcessor;
        private CaptureEvents _capture;

        private readonly SemaphoreSlim _syncLock = new(1);

        private bool _isRunning = false;
        protected bool _isDraining;

        public MediaStream(IAzureSettings settings, IGraphLogger logger, string mediaId)
        {
            _settings = (AzureSettings)settings;
            _logger = logger;
            _mediaId = mediaId;

            _tokenSource = new CancellationTokenSource();

            _buffer = new BufferBlock<SerializableAudioMediaBuffer>(new DataflowBlockOptions { CancellationToken = _tokenSource.Token });
            _currentAudioProcessor = new AudioProcessor(_settings);

            if (_settings.CaptureEvents)
            {
                _capture = new CaptureEvents(Path.Combine(Path.GetTempPath(), BotConstants.DEFAULT_OUTPUT_FOLDER, _settings.EventsFolder, _mediaId, "media"));
            }
        }

        public async Task AppendAudioBuffer(AudioMediaBuffer buffer, List<IParticipant> participants)
        {
            if (!_isRunning)
            {
                await Start().ConfigureAwait(false);
            }

            try
            {
                await _buffer.SendAsync(new SerializableAudioMediaBuffer(buffer, participants), _tokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                _buffer?.Complete();
                _logger.Error(e, "Cannot enqueue because queuing operation has been cancelled");
            }
        }

        private async Task Start()
        {
            await _syncLock.WaitAsync().ConfigureAwait(false);

            if (!_isRunning)
            {
                await Task.Run(Process).ConfigureAwait(false);

                _isRunning = true;
            }

            _syncLock.Release();
        }

        private async Task Process()
        {
            if (_settings.CaptureEvents && !_isDraining && _capture == null)
            {
                _capture = new CaptureEvents(Path.Combine(Path.GetTempPath(), BotConstants.DEFAULT_OUTPUT_FOLDER, _settings.EventsFolder, _mediaId, "media"));
            }

            try
            {
                while (await _buffer.OutputAvailableAsync(_tokenSource.Token).ConfigureAwait(false))
                {
                    SerializableAudioMediaBuffer data = await _buffer.ReceiveAsync(_tokenSource.Token).ConfigureAwait(false);

                    if (_settings.CaptureEvents)
                    {
                        await _capture?.Append(data);
                    }
                        
                    await _currentAudioProcessor.Append(data);

                    _tokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error(ex, "The queue processing task has been cancelled.");
            }
            catch (ObjectDisposedException ex)
            {
                _logger.Error(ex, "The queue processing task object has been disposed.");
            }
            catch (Exception ex)
            {
                // Catch all other exceptions and log
                _logger.Error(ex, "Caught Exception");

                // Continue processing elements in the queue
                await Process().ConfigureAwait(false);
            }

            //send final segment as a last precation in case the loop did not process it
            if (_currentAudioProcessor != null)
            {
                await ChunkProcess().ConfigureAwait(false);
            }

            if (_settings.CaptureEvents)
            {
                await _capture.Finalize().ConfigureAwait(false);
            }

            _isDraining = false;
        }

        public async Task End()
        {
            if (!_isRunning)
            {
                return;
            }

            await _syncLock.WaitAsync().ConfigureAwait(false);

            if (_isRunning)
            {
                _isDraining = true;
                while (_buffer.Count > 0)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }

                _buffer.Complete();
                _buffer.TryDispose();
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _isRunning = false;

                while (_isDraining)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }

            _syncLock.Release();
        }

        async Task ChunkProcess()
        {
            try
            {
                var finalData = await _currentAudioProcessor.Finalize().ConfigureAwait(false);
                _logger.Info($"Recording saved to: {finalData}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Caught exception while processing chunck.");
            }
        }
    }
}
