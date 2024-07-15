// <copyright file="AudioBufferAsyncWriter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;

    /// <summary>
    /// Handles writing audio buffers asynchronously so that the media SDK thread delivering
    /// the buffers is not blocked waiting for non-negligible time such as for I/O, encryption,
    /// locking, etc.
    /// Also, monitors the audio buffer rate and sends a custom event telemetry if it drops below
    /// the expected rate of 50 frames/second.
    /// </summary>
    public class AudioBufferAsyncWriter : IAsyncDisposable
    {
        private readonly BufferBlock<Action> audioStreamActionsQueue = new BufferBlock<Action>();
        private readonly CancellationTokenSource audioMediaQueueReaderCts = new CancellationTokenSource();
        private readonly ICall call;
        private Task audioMediaQueueReaderTask;
        private bool disposed;
        private MediaFileWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBufferAsyncWriter"/> class.
        /// </summary>
        /// <param name="call">The call object.</param>
        /// <param name="disableAudioStreamIo">Whether to write audio to a file or not.</param>
        public AudioBufferAsyncWriter(ICall call, bool disableAudioStreamIo)
        {
            if (disableAudioStreamIo)
            {
                return;
            }

            this.call = call ?? throw new ArgumentNullException(nameof(call));
            string callTempLocation = @"C:\samplebot\captures";
            Directory.CreateDirectory(callTempLocation);
            this.writer = this.GetWriter($"{Path.Combine(callTempLocation, Guid.NewGuid().ToString())}");
        }

        /// <summary>
        /// Gets a value indicating whether audio buffer started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// starts audio buffer writer.
        /// </summary>
        public void Start()
        {
            this.audioMediaQueueReaderTask = this.AudioBufferQueueReaderLoopAsync();
            this.IsStarted = true;
        }

        /// <summary>
        /// enques the buffer to queue.
        /// </summary>
        /// <param name="buffer">buffer.</param>
        public void EnqueueBuffer(AudioMediaBuffer buffer)
        {
            if (!this.audioStreamActionsQueue.Post(() => this.WriteDequeuedAudioBufferAsync(buffer).ConfigureAwait(true)))
            {
                this.call.GraphLogger.Warn("Failed to post action to write audio buffer to queue");
            }
        }

        /// <summary>
        /// Almost like <see cref="DisposeAsync"/> but doesn't wait too long for the queue to be empty.
        /// This is used when Conditional Recording stops.
        /// </summary>
        public void FastClose()
        {
            if (this.audioMediaQueueReaderTask != null)
            {
                this.audioStreamActionsQueue.Complete();
                this.audioMediaQueueReaderCts.CancelAfter(TimeSpan.FromMilliseconds(20)); // we wait for max 20ms.
                this.audioMediaQueueReaderTask.Wait();
                this.audioMediaQueueReaderTask.Dispose();
            }

            this.audioMediaQueueReaderCts.Dispose();
            this.disposed = true;
        }

        /// <summary>
        /// Dispose audio buffer queue.
        /// </summary>
        /// <returns>value task.</returns>
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.audioMediaQueueReaderTask != null)
            {
                this.audioStreamActionsQueue.Complete();
                await this.audioStreamActionsQueue.Completion.ConfigureAwait(true);
                this.audioMediaQueueReaderCts.Cancel();
                this.audioMediaQueueReaderTask.Wait();
                this.audioMediaQueueReaderTask.Dispose();
            }

            this.audioMediaQueueReaderCts.Dispose();
            this.disposed = true;
        }

        /// <summary>
        /// Create media file writer.
        /// </summary>
        /// <param name="filePath"> file path.</param>
        /// <returns>Returns media file writer.</returns>
        public MediaFileWriter GetWriter(string filePath)
        {
            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            var bufferedStream = new BufferedStream(fileStream);
            return new MediaFileWriter(bufferedStream);
        }

        /// <summary>
        /// AudioBufferQueueReaderLoopAsync.
        /// </summary>
        /// <returns>returns task.</returns>
        private async Task AudioBufferQueueReaderLoopAsync()
        {
            try
            {
                while (await this.audioStreamActionsQueue.OutputAvailableAsync(this.audioMediaQueueReaderCts.Token).ConfigureAwait(true))
                {
                    var action = await this.audioStreamActionsQueue.ReceiveAsync(this.audioMediaQueueReaderCts.Token).ConfigureAwait(true);
                    action();
                }
            }
            catch (OperationCanceledException)
            {
                if (this.audioStreamActionsQueue.Count > 0)
                {
                    this.call.GraphLogger.Warn(
                        $"Cancelled before we could write all the audio buffers from the queue. Remaining count: {this.audioStreamActionsQueue.Count}");
                }
                else
                {
                    this.call.GraphLogger.Info("Finished consuming the audio buffer queue.");
                }
            }
            catch (InvalidOperationException)
            {
                this.call.GraphLogger.Warn(
                    "Got InvalidOperationException possibly because BlockBuffer is in completed state.");
            }
            catch (Exception ex)
            {
                this.call.GraphLogger.Error(ex, "Caught exception while consuming the audio stream actions queue.");
            }

            //// drain any remaining buffer rate monitoring data from the end
            // try
            // {
            //    if (mixedAudioBufferGaps > 0)
            //    {
            //        SendBufferRateDropEvent(_audioBufferPeriodsElapsed,
            //            _audioBufferPeriodsElapsed - _mixedAudioBufferGaps);
            //    }
            // }
            // catch (Exception ex)
            // {
            //    call.GraphLogger.Error($"Error during audio buffer rate drop monitoring. Exception: {ex}");
            // }
        }

        /// <summary>
        /// WriteDequeuedAudioBuffer.
        /// </summary>
        /// <param name="buffer">Audio media buffer.</param>
        private void WriteDequeuedAudioBuffer(AudioMediaBuffer buffer)
        {
            try
            {
#pragma warning disable IDISP007 // Don't dispose injected
                // IAudioSocket.AudioMediaReceived documentation clearly states that application is responsible for calling buffer's Dispose.
                // So, this is not a violation
                using (buffer)
#pragma warning restore IDISP007 // Don't dispose injected
                {
                    // var writer = this.GetWriter($"{Path.Combine(this.callTempLocation, Guid.NewGuid().ToString())}");
                    this.writer.Write(buffer.Timestamp, buffer.Data, buffer.Length);
                }
            }
            catch (Exception err)
            {
                // fatalExceptionCallback(err);
                this.call.GraphLogger.Error($"Error during audio buffer rate drop monitoring. Exception: {err}");
            }
        }

        /// <summary>
        /// WriteDequeuedAudioBufferAsync.
        /// </summary>
        /// <param name="buffer">Audio media buffer.</param>
        /// <returns>returns task.</returns>
        private async Task WriteDequeuedAudioBufferAsync(AudioMediaBuffer buffer)
        {
            try
            {
                using (buffer)
                {
                    byte[] data = new byte[buffer.Length];
                    unsafe
                    {
                        System.Runtime.InteropServices.Marshal.Copy(new IntPtr((byte*)buffer.Data), data, 0, (int)buffer.Length);
                    }

                    await this.writer.WriteAsync(buffer.Timestamp, data).ConfigureAwait(false);
                }
            }
            catch (Exception err)
            {
                this.call.GraphLogger.Error($"Error during audio buffer rate drop monitoring. Exception: {err}");
            }
        }
    }
}