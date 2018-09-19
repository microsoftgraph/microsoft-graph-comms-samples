﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.Skype.Internal.Media.H264;
    using Sample.Common.Logging;

    /// <summary>
    /// The utility class.
    /// </summary>
    internal static class Utilities
    {
        private const double TicksInOneMs = 10000.0;
        private const double MsInOneSec = 1000.0;
        private static readonly ConcurrentDictionary<int, List<H264Frame>> H264Frames;

        /// <summary>
        /// Initializes static members of the <see cref="Utilities"/> class.
        /// Initializes the <see cref="Utilities"/> class.
        /// </summary>
        static Utilities()
        {
            H264Frames = new ConcurrentDictionary<int, List<H264Frame>>();
            foreach (var videoFormatEntry in Service.Instance.Configuration.H264FileLocations)
            {
                var videoFormat = videoFormatEntry.Value;
                var fileReader = new H264FileReader(
                                    videoFormatEntry.Key,
                                    (uint)videoFormat.Width,
                                    (uint)videoFormat.Height,
                                    videoFormat.FrameRate);

                var listOfFrames = new List<H264Frame>();
                var totalNumberOfFrames = fileReader.GetTotalNumberOfFrames();
                Log.Info(new CallerInfo(), LogContext.Media, $"Found the fileReader for the format with id: {videoFormat.GetId()} and number of frames {totalNumberOfFrames}");

                for (int i = 0; i < totalNumberOfFrames; i++)
                {
                    H264Frame frame = new H264Frame();
                    fileReader.GetNextFrame(frame);
                    listOfFrames.Add(frame);
                }

                H264Frames.TryAdd(videoFormat.GetId(), listOfFrames);
            }
        }

        /// <summary>
        /// Extension for Task to execute the task in background and log any exception.
        /// </summary>
        /// <param name="task">Task to execute and capture any exceptions.</param>
        /// <param name="description">Friendly description of the task for debugging purposes.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task ForgetAndLogExceptionAsync(this Task task, string description = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // ignore
                Log.Error(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    "Caught an Exception running the task: {0} {1}\n StackTrace: {2}",
                    description ?? string.Empty,
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Creates the video buffers from the provided h264 files.
        /// </summary>
        /// <param name="currentTick">The number of ticks that represent the current date and time.</param>
        /// <param name="videoFormats">The encoded video source formats.</param>
        /// <param name="replayed">If the video frame is being replayed.</param>
        /// <returns>The newly created list of <see cref="VideoMediaBuffer"/>.</returns>
        public static List<VideoMediaBuffer> CreateVideoMediaBuffers(
            long currentTick,
            List<VideoFormat> videoFormats,
            bool replayed)
        {
            List<VideoMediaBuffer> videoMediaBuffers = new List<VideoMediaBuffer>();
            try
            {
                foreach (var videoFormat in videoFormats)
                {
                    if (H264Frames.TryGetValue(videoFormat.GetId(), out List<H264Frame> h264Frames))
                    {
                        // create the videoBuffers
                        var packetSizeInMs = (long)((MsInOneSec / videoFormat.FrameRate) * TicksInOneMs);
                        var referenceTime = currentTick;

                        if (replayed)
                        {
                            referenceTime += packetSizeInMs;
                        }

                        foreach (var h264Frame in h264Frames)
                        {
                            var frameSize = h264Frame.Size;
                            byte[] buffer = new byte[frameSize];
                            Marshal.Copy(h264Frame.Data, buffer, 0, (int)frameSize);
                            videoMediaBuffers.Add(new VideoSendBuffer(buffer, (uint)buffer.Length, videoFormat, referenceTime));
                            referenceTime += packetSizeInMs;
                        }
                    }
                    else
                    {
                        Log.Error(new CallerInfo(), LogContext.Media, $"h264FileReader not found for the videoFromat {videoFormat}");
                    }
                }

                Log.Info(new CallerInfo(), LogContext.Media, "created {0} VideoMediaBuffers", videoMediaBuffers.Count);
                return videoMediaBuffers;
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.Media, "Failed to create the videoMediaBuffers with exception {0}", ex);
            }

            return videoMediaBuffers;
        }

        /// <summary>
        /// Helper function to create the audio buffers from file.
        /// Please make sure the audio file provided is PCM16Khz and the fileSizeInSec is the correct length.
        /// </summary>
        /// <param name="currentTick">The current clock tick.</param>
        /// <param name="replayed">Whether it's replayed.</param>
        /// <returns>The newly created list of <see cref="AudioMediaBuffer"/>.</returns>
        public static List<AudioMediaBuffer> CreateAudioMediaBuffers(long currentTick, bool replayed)
        {
            var audioMediaBuffers = new List<AudioMediaBuffer>();
            var referenceTime = currentTick;

            // packet size of 20 ms
            var numberOfTicksInOneAudioBuffers = 20 * 10000;
            if (replayed)
            {
                referenceTime += numberOfTicksInOneAudioBuffers;
            }

            using (FileStream fs = File.Open(Service.Instance.Configuration.AudioFileLocation, FileMode.Open))
            {
                byte[] bytesToRead = new byte[640];

                // skipping the wav headers
                fs.Seek(44, SeekOrigin.Begin);
                while (fs.Read(bytesToRead, 0, bytesToRead.Length) >= 640)
                {
                    // here we want to create buffers of 20MS with PCM 16Khz
                    IntPtr unmanagedBuffer = Marshal.AllocHGlobal(640);
                    Marshal.Copy(bytesToRead, 0, unmanagedBuffer, 640);
                    var audioBuffer = new AudioSendBuffer(unmanagedBuffer, 640, AudioFormat.Pcm16K, referenceTime);
                    audioMediaBuffers.Add(audioBuffer);
                    referenceTime += numberOfTicksInOneAudioBuffers;
                }
            }

            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "created {0} AudioMediaBuffers",
                audioMediaBuffers.Count);
            return audioMediaBuffers;
        }

        /// <summary>
        /// Helper function get id.
        /// </summary>
        /// <param name="videoFormat">Video format.</param>
        /// <returns>The <see cref="int"/> of the video format.</returns>
        public static int GetId(this VideoFormat videoFormat)
        {
            return $"{videoFormat.VideoColorFormat}{videoFormat.Width}{videoFormat.Height}".GetHashCode();
        }
    }
}
