// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="MediaStream.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
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
    /// <summary>
    /// Class MediaStream.
    /// Implements the <see cref="RecordingBot.Services.Contract.IMediaStream" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Contract.IMediaStream" />
    public class MediaStream : IMediaStream
    {
        /// <summary>
        /// The buffer
        /// </summary>
        private BufferBlock<SerializableAudioMediaBuffer> _buffer;
        /// <summary>
        /// The token source
        /// </summary>
        private CancellationTokenSource _tokenSource;
        /// <summary>
        /// The is the indicator if the media stream is running
        /// </summary>
        private bool _isRunning = false;
        /// <summary>
        /// The is draining indicator
        /// </summary>
        protected bool _isDraining;

        /// <summary>
        /// The synchronize lock
        /// </summary>
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1);
        /// <summary>
        /// The media identifier
        /// </summary>
        private readonly string _mediaId;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly AzureSettings _settings;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        /// <summary>
        /// The current audio processor
        /// </summary>
        private AudioProcessor _currentAudioProcessor;
        /// <summary>
        /// The capture
        /// </summary>
        private CaptureEvents _capture;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream" /> class.

        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="mediaId">The media identifier.</param>
        public MediaStream(IAzureSettings settings, IGraphLogger logger, string mediaId)
        {
            _settings = (AzureSettings)settings;
            _logger = logger;
            _mediaId = mediaId;
        }

        /// <summary>
        /// Appends the audio buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="participants">The participants.</param>
        public async Task AppendAudioBuffer(AudioMediaBuffer buffer, List<IParticipant> participants)
        {
            if (!_isRunning)
            {
                await _start();
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


        /// <summary>
        /// Ends this instance.
        /// </summary>
        /// <returns>Task.</returns>
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
                _buffer = null;
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

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private async Task _start()
        {
            await this._syncLock.WaitAsync().ConfigureAwait(false);
            if (!_isRunning)
            {
                _tokenSource = new CancellationTokenSource();
                _buffer = new BufferBlock<SerializableAudioMediaBuffer>(new DataflowBlockOptions { CancellationToken = this._tokenSource.Token });
                await Task.Factory.StartNew(this._process).ConfigureAwait(false);
                _isRunning = true;
            }
            this._syncLock.Release();
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        private async Task _process()
        {
            _currentAudioProcessor = new AudioProcessor(_settings);

            if (_settings.CaptureEvents && !_isDraining && _capture == null)
            {
                _capture = new CaptureEvents(Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, _settings.EventsFolder, _mediaId, "media"));
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
                await _process().ConfigureAwait(false);
            }

            //send final segment as a last precation in case the loop did not process it
            if (_currentAudioProcessor != null)
            {
                await _chunkProcess();
            }

            if (_settings.CaptureEvents)
            {
                await _capture?.Finalise();
            }

            _isDraining = false;
        }

        /// <summary>
        /// Chunks the process.
        /// </summary>
        async Task _chunkProcess()
        {
            try
            {
                var finalData = await _currentAudioProcessor.Finalise();
                _logger.Info($"Recording saved to: {finalData}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Caught exception while processing chunck.");
            }
        }
    }
}
