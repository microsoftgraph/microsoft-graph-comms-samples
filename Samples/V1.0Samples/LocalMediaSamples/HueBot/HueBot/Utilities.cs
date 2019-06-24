// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.HueBot
{
    using System.Runtime.InteropServices;
    using Microsoft.Skype.Bots.Media;
    using Sample.HueBot.Bot;

    /// <summary>
    /// The utility class.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// The get sending video format.
        /// </summary>
        /// <param name="videoFormat">The video format.</param>
        /// <returns>
        /// The <see cref="VideoFormat" />.
        /// </returns>
        public static VideoFormat GetSendVideoFormat(this VideoFormat videoFormat)
        {
            VideoFormat sendVideoFormat;
            switch (videoFormat.Width)
            {
                case 270:
                    sendVideoFormat = VideoFormat.NV12_270x480_15Fps;
                    break;

                case 320:
                    sendVideoFormat = VideoFormat.NV12_320x180_15Fps;
                    break;

                case 360:
                    sendVideoFormat = VideoFormat.NV12_360x640_15Fps;
                    break;

                case 424:
                    sendVideoFormat = VideoFormat.NV12_424x240_15Fps;
                    break;

                case 480:
                    if (videoFormat.Height == 270)
                    {
                        sendVideoFormat = VideoFormat.NV12_480x270_15Fps;
                        break;
                    }

                    sendVideoFormat = VideoFormat.NV12_480x848_30Fps;
                    break;

                case 640:
                    sendVideoFormat = VideoFormat.NV12_640x360_15Fps;
                    break;

                case 720:
                    sendVideoFormat = VideoFormat.NV12_720x1280_30Fps;
                    break;

                case 848:
                    sendVideoFormat = VideoFormat.NV12_848x480_30Fps;
                    break;

                case 960:
                    sendVideoFormat = VideoFormat.NV12_960x540_30Fps;
                    break;

                case 1280:
                    sendVideoFormat = VideoFormat.NV12_1280x720_30Fps;
                    break;

                case 1920:
                    sendVideoFormat = VideoFormat.NV12_1920x1080_30Fps;
                    break;

                default:
                    sendVideoFormat = VideoFormat.NV12_424x240_15Fps;
                    break;
            }

            return sendVideoFormat;
        }

        /// <summary>
        /// Applies the hue colour onto the specified buffer.
        /// </summary>
        /// <param name="videoMediaBuffer">The video media buffer.</param>
        /// <param name="hueColor">Color of the hue.</param>
        /// <returns>The media buffer byte array.</returns>
        public static byte[] ApplyHue(this VideoMediaBuffer videoMediaBuffer, CallHandler.HueColor hueColor)
        {
            byte[] buffer = new byte[videoMediaBuffer.VideoFormat.Width * videoMediaBuffer.VideoFormat.Height * 12 / 8];
            Marshal.Copy(videoMediaBuffer.Data, buffer, 0, buffer.Length);
            ApplyHue(buffer, hueColor, videoMediaBuffer.VideoFormat.Width, videoMediaBuffer.VideoFormat.Height);
            return buffer;
        }

        /// <summary>
        /// Applies the hue colour onto the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="hueColor">The hue color.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        private static void ApplyHue(byte[] buffer, CallHandler.HueColor hueColor, int width, int height)
        {
            int widthXheight = width * height;
            for (var index = widthXheight; index < widthXheight * 3 / 2; index += 2)
            {
                switch (hueColor)
                {
                    case CallHandler.HueColor.Red:
                        AddWithoutRollover(buffer, index, -16);
                        AddWithoutRollover(buffer, index + 1, 50);
                        break;

                    case CallHandler.HueColor.Blue:
                        AddWithoutRollover(buffer, index, 50);
                        AddWithoutRollover(buffer, index + 1, -8);
                        break;

                    case CallHandler.HueColor.Green:
                        AddWithoutRollover(buffer, index, -33);
                        AddWithoutRollover(buffer, index + 1, -41);
                        break;

                    default: break;
                }
            }
        }

        /// <summary>
        /// subtract without rollover.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        private static void AddWithoutRollover(byte[] buffer, int index, int value)
        {
            buffer[index] = (byte)(value + buffer[index]).Clamp(0, byte.MaxValue);
        }
    }
}
