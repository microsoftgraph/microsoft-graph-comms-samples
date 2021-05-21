// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MediaFrameSourceComponent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
// Media frame source \psi component.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PsiBot.FrontEnd
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Skype.Bots.Media;
    using Sample.PsiBot.FrontEnd.Bot;

    /// <summary>
    /// Media frame source component.
    /// </summary>
    public class MediaFrameSourceComponent : ISourceComponent
    {
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
            this.callHandler = callHandler;
            this.logger = logger;
            this.logPrefix = $"[{nameof(MediaFrameSourceComponent)} {pipeline.Name}]";

            this.Audio = pipeline.CreateEmitter<Dictionary<string, AudioBuffer>>(this, "Audio Emitter");
            this.Video = pipeline.CreateEmitter<Dictionary<string, Shared<Image>>>(this, "Image Emitter");
        }

        /// <summary>
        /// Gets audio stream.
        /// </summary>
        public Emitter<Dictionary<string, AudioBuffer>> Audio { get; }

        /// <summary>
        /// Gets video stream.
        /// </summary>
        public Emitter<Dictionary<string, Shared<Image>>> Video { get; }

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
                this.logger.Warn($"Audio frame timestamp is zero.");
                return;
            }

            var audioFormat = audioFrame.AudioFormat == Microsoft.Skype.Bots.Media.AudioFormat.Pcm44KStereo ? Microsoft.Psi.Audio.WaveFormat.Create16BitPcm(44000, 2) : Microsoft.Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm();
            var buffers = new Dictionary<string, AudioBuffer>();

            var originatingTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(audioFrame.Timestamp);

            if (audioFrame.UnmixedAudioBuffers != null)
            {
                foreach (var buffer in audioFrame.UnmixedAudioBuffers)
                {
                    var length = buffer.Length;
                    var data = new byte[length];
                    Marshal.Copy(buffer.Data, data, 0, (int)length);
                    var participant = this.callHandler.GetParticipantFromMSI(buffer.ActiveSpeakerId);
                    if (participant != null)
                    {
                        buffers.Add(participant.Resource.Info.Identity.User.Id, new AudioBuffer(data, audioFormat));
                    }
                    else
                    {
                        this.logger.Warn($"Couldn't find participant for ActiveSpeakerId: {buffer.ActiveSpeakerId}");
                    }
                }
            }

            lock (this.Audio)
            {
                if (originatingTime > this.Audio.LastEnvelope.OriginatingTime)
                {
                    this.Audio.Post(buffers, originatingTime);
                }
                else
                {
                    this.logger.Warn($"Dropped out-of-order audio frame set");
                }
            }
        }

        /// <summary>
        /// Received image frame.
        /// </summary>
        /// <param name="videoFrame">Video frame.</param>
        /// <param name="id">Video frame MSI.</param>
        public void Received(VideoMediaBuffer videoFrame, uint id)
        {
            var timestamp = (long)videoFrame.Timestamp;
            if (timestamp == 0)
            {
                this.logger.Warn($"Original sender timestamp is zero: {id}");
                return;
            }

            var videoFormat = videoFrame.VideoFormat;

            var length = videoFormat.Width * videoFormat.Height * 12 / 8; // This is how to calculate NV12 buffer size
            if (length > videoFrame.Length)
            {
                return;
            }

            byte[] data = new byte[length];
            Marshal.Copy(videoFrame.Data, data, 0, (int)length);

            using (var sharedImage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(
                videoFormat.Width,
                videoFormat.Height,
                Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp))
            {
                var bgr = NV12toBGR(data, videoFormat.Width, videoFormat.Height);

                sharedImage.Resource.CopyFrom(bgr);

                var streams = new Dictionary<string, Shared<Image>>();
                var originatingTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(timestamp);
                var participant = this.callHandler.GetParticipantFromMSI(id);
                if (participant != null)
                {
                    streams.Add(participant.Resource.Info.Identity.User.Id, sharedImage);
                }
                else
                {
                    this.logger.Warn($"Couldn't find participant for media source ID: {id}");
                }

                lock (this.Video)
                {
                    if (originatingTime > this.Video.LastEnvelope.OriginatingTime)
                    {
                        this.Video.Post(streams, originatingTime);
                    }
                    else
                    {
                        this.logger.Warn($"Dropped out-of-order video frame: {id}");
                    }
                }
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
