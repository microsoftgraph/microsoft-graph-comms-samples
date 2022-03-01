// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;

namespace EchoBot.Services.Util
{
    /// <summary>
    /// The utility class.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Helper function to create the audio buffers from file.
        /// Please make sure the audio file provided is PCM16Khz and the fileSizeInSec is the correct length.
        /// </summary>
        /// <param name="currentTick">The current clock tick.</param>
        /// <param name="replayed">Whether it's replayed.</param>
        /// <param name="logger">Graph logger.</param>
        /// <returns>The newly created list of <see cref="AudioMediaBuffer"/>.</returns>
        public static List<AudioMediaBuffer> CreateAudioMediaBuffers(AudioDataStream stream, long currentTick, ILogger logger)
        {
            var audioMediaBuffers = new List<AudioMediaBuffer>();
            var referenceTime = currentTick;

            // packet size of 20 ms
            var numberOfTicksInOneAudioBuffers = 20 * 10000;

            byte[] bytesToRead = new byte[640];

            // skipping the wav headers
            stream.SetPosition(44);
            while(stream.ReadData(bytesToRead) >= 640)
            {
                // here we want to create buffers of 20MS with PCM 16Khz
                IntPtr unmanagedBuffer = Marshal.AllocHGlobal(640);
                Marshal.Copy(bytesToRead, 0, unmanagedBuffer, 640);
                var audioBuffer = new AudioSendBuffer(unmanagedBuffer, 640, AudioFormat.Pcm16K, referenceTime);
                audioMediaBuffers.Add(audioBuffer);
                referenceTime += numberOfTicksInOneAudioBuffers;
            }

            logger.LogTrace($"created {audioMediaBuffers.Count} AudioMediaBuffers");
            return audioMediaBuffers;
        }

        public static List<AudioMediaBuffer> CreateAudioMediaBuffers(byte[] buffer, long currentTick, ILogger logger)
        {
            var audioMediaBuffers = new List<AudioMediaBuffer>();
            var referenceTime = currentTick;

            // packet size of 20 ms
            var numberOfTicksInOneAudioBuffers = 20 * 10000;

            // here we want to create buffers of 20MS with PCM 16Khz
            IntPtr unmanagedBuffer = Marshal.AllocHGlobal(640);
            Marshal.Copy(buffer, 0, unmanagedBuffer, 640);
            var audioBuffer = new AudioSendBuffer(unmanagedBuffer, 640, AudioFormat.Pcm16K, referenceTime);
            audioMediaBuffers.Add(audioBuffer);
            referenceTime += numberOfTicksInOneAudioBuffers;

            logger.LogTrace($"created {audioMediaBuffers.Count} AudioMediaBuffers");
            return audioMediaBuffers;
        }
    }
}
