// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MediaFrameSourceComponent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
// Media frame source \psi component.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace PsiBot.Services.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Skype.Bots.Media;

    /// <summary>
    /// Media frame source component.
    /// </summary>
    public class MediaFrameSourceComponent : ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly CallHandler callHandler;

        private readonly IGraphLogger logger;
        private readonly string logPrefix;

        private bool started = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrameSourceComponent"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline in which this component lives.</param>
        /// <param name="callHandler">Call handler.</param>
        /// <param name="logger">Graph logger.</param>
        public MediaFrameSourceComponent(Pipeline pipeline, CallHandler callHandler, IGraphLogger logger)
        {
            this.pipeline = pipeline;
            this.callHandler = callHandler;
            this.logger = logger;
            this.logPrefix = $"[{nameof(MediaFrameSourceComponent)} {this.pipeline.Name}]";

            this.Audio = this.pipeline.CreateEmitter<Dictionary<string, (AudioBuffer, DateTime)>>(this, nameof(this.Audio));
            this.Video = this.pipeline.CreateEmitter<Dictionary<string, (Shared<Image>, DateTime)>>(this, nameof(this.Video));
        }

        /// <summary>
        /// Gets audio stream.
        /// </summary>
        public Emitter<Dictionary<string, (AudioBuffer, DateTime)>> Audio { get; }

        /// <summary>
        /// Gets video stream.
        /// </summary>
        public Emitter<Dictionary<string, (Shared<Image>, DateTime)>> Video { get; }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.logger.Verbose($"{this.logPrefix} (started = {this.started}) Start called.");
            notifyCompletionTime(DateTime.MaxValue); // Notify that this is an infinite source component
            this.started = true;
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.logger.Verbose($"{this.logPrefix} (started = {this.started}) Stop called.");
            this.started = false;
            notifyCompleted(); // No more messages will be posted after this.started = false
        }

        /// <summary>
        /// Received audio.
        /// </summary>
        /// <param name="audioFrame">Audio buffer.</param>
        public void Received(AudioMediaBuffer audioFrame)
        {
            if (audioFrame.Timestamp == 0)
            {
                this.logger.Warn($"Audio buffer timestamp is zero.");
                return;
            }

            var audioFormat = audioFrame.AudioFormat == Microsoft.Skype.Bots.Media.AudioFormat.Pcm44KStereo ?
                Microsoft.Psi.Audio.WaveFormat.Create16BitPcm(44000, 2) :
                Microsoft.Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm();
            
            var buffers = new Dictionary<string, (AudioBuffer, DateTime)>();
            var audioFrameTimestamp = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(audioFrame.Timestamp);

            if (audioFrame.UnmixedAudioBuffers != null)
            {
                foreach (var buffer in audioFrame.UnmixedAudioBuffers)
                {
                    var length = buffer.Length;
                    var data = new byte[length];
                    Marshal.Copy(buffer.Data, data, 0, (int)length);

                    var participant = CallHandler.GetParticipantFromMSI(this.callHandler.Call, buffer.ActiveSpeakerId);
                    var identity = CallHandler.TryGetParticipantIdentity(participant);
                    if (identity != null)
                    {
                        
                        buffers.Add(identity.Id, (new AudioBuffer(data, audioFormat), audioFrameTimestamp));
                    }
                    else
                    {
                        this.logger.Warn($"Couldn't find participant for ActiveSpeakerId: {buffer.ActiveSpeakerId}");
                    }
                }
            }

            lock (this.Audio)
            {
                this.Audio.Post(buffers, this.pipeline.GetCurrentTime());
            }
        }

        /// <summary>
        /// Received image frame.
        /// </summary>
        /// <param name="videoFrame">Video frame.</param>
        /// <param name="id">Video frame MSI.</param>
        public void Received(VideoMediaBuffer videoFrame, uint id)
        {
            if (videoFrame.Timestamp == 0)
            {
                this.logger.Warn($"Video buffer timestamp is zero from sender: {id}");
                return;
            }

            var videoFormat = videoFrame.VideoFormat;
            var videoFrameTimestamp = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(videoFrame.Timestamp);

            var length = videoFormat.Width * videoFormat.Height * 12 / 8; // This is how to calculate NV12 buffer size
            if (length > videoFrame.Length)
            {
                this.logger.Warn($"Length of video frame not as expected: {id}");
                return;
            }

            byte[] data = new byte[length];
            Marshal.Copy(videoFrame.Data, data, 0, (int)length);

            using var sharedImage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(
                videoFormat.Width,
                videoFormat.Height,
                Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);

            var bgr = NV12toBGR(data, videoFormat.Width, videoFormat.Height);
            sharedImage.Resource.CopyFrom(bgr);

            var streams = new Dictionary<string, (Shared<Image>, DateTime)>();
            var participant = CallHandler.GetParticipantFromMSI(this.callHandler.Call, id);
            var identity = CallHandler.TryGetParticipantIdentity(participant);
            if (identity != null)
            {
                streams.Add(identity.Id, (sharedImage, videoFrameTimestamp));
            }
            else
            {
                this.logger.Warn($"Couldn't find participant for media source ID: {id}");
            }

            lock(this.Video)
            {
                this.Video.Post(streams, this.pipeline.GetCurrentTime());
            }
        }

        /// <summary>
        /// Converts an NV12 byte array to BGR format.
        /// </summary>
        /// <param name="array">The input array.</param>
        /// <param name="width">The input width.</param>
        /// <param name="height">The input height.</param>
        /// <param name="destWidth">The optional output width. Leave 0 to use the same as the input width.</param>
        /// <param name="destHeight">The optional output height. Leave 0 to use the same as the input height.</param>
        /// <returns>The BGR image array.</returns>
        private static byte[] NV12toBGR(byte[] array, int width, int height, int destWidth = 0, int destHeight = 0)
        {
            if (destWidth == 0)
            {
                destWidth = width;
            }

            if (destHeight == 0)
            {
                destHeight = height;
            }

            // The stride needs to be rounded up to the nearest multiple of 4 bytes.
            // The first byte of each scan line is aligned on a 32-bit address boundary for performance.
            // The below integer arithmetic, 4 * (<width_in_bytes> + 3) / 4), is a standard means of including padding as needed.
            int resultStride = 4 * (((3 * destWidth) + 3) / 4);
            byte[] result = new byte[destHeight * resultStride];
            int startUV = width * height;
            int strideUV = width;

            // Get the resize factors
            double widthFactor = (double)destWidth / width;
            double heightFactor = (double)destHeight / height;

            for (int i = 0; i < height; i++)
            {
                int row = i * width;
                for (int j = 0; j < width; j++)
                {
                    byte y = array[row + j];
                    byte u = array[startUV + ((i / 2) * strideUV) + (2 * (j / 2))];
                    byte v = array[startUV + ((i / 2) * strideUV) + (2 * (j / 2)) + 1];

                    // https://www.fourcc.org/fccyvrgb.php  Several options we can explore which is best.
                    byte b = (byte)Math.Max(0, Math.Min(255, (1.164 * (y - 16)) + (2.018 * (u - 128))));
                    byte g = (byte)Math.Max(0, Math.Min(255, (1.164 * (y - 16)) - (0.813 * (v - 128)) - (0.391 * (u - 128))));
                    byte r = (byte)Math.Max(0, Math.Min(255, (1.164 * (y - 16)) + (1.596 * (v - 128))));

                    for (int destY = (int)(i * heightFactor); destY < (i + 1) * heightFactor; destY++)
                    {
                        for (int destX = (int)(j * widthFactor); destX < (j + 1) * widthFactor; destX++)
                        {
                            // windows bitmaps are actually BGR.
                            result[(destY * resultStride) + (3 * destX)] = b;
                            result[(destY * resultStride) + (3 * destX) + 1] = g;
                            result[(destY * resultStride) + (3 * destX) + 2] = r;
                        }
                    }
                }
            }

            return result;
        }
    }
}
