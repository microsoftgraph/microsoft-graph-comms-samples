// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#pragma warning disable
namespace Sample.HueBot
{
    using System;
    using System.Drawing;
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

            // byte[] buffer = CreateGreenYUV(videoMediaBuffer.VideoFormat.Width, videoMediaBuffer.VideoFormat.Height);
            // ApplyHue(buffer, hueColor, videoMediaBuffer.VideoFormat.Width, videoMediaBuffer.VideoFormat.Height);
            // DoubleFrame(buffer, videoMediaBuffer.VideoFormat.Width, videoMediaBuffer.VideoFormat.Height);

            var yuv = new YUV(buffer, videoMediaBuffer.VideoFormat.Width, videoMediaBuffer.VideoFormat.Height, videoMediaBuffer.Stride);

            //if (DateTime.Now.Second < 30)
            //    PictureOnMono(yuv, 3);
            //else
            //    PictureInPicture(yuv, 3);

            PresenterView(yuv);

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

        /// <summary>
        /// Double.
        /// </summary>
        private static void DoubleFrame(byte[] buffer, int width, int height)
        {
            int cut = width / 4;
            for (var x = 0; x < width / 2; x += 1)
            {
                for (var y = 0; y < height; y += 1)
                {
                    var i = (width * y) + x;
                    buffer[i] = buffer[i + cut];
                    buffer[i + (width / 2)] = buffer[i + cut];
                }
            }
        }

        private static void SolidFill(YUV input, byte red, byte green, byte blue)
        {
            var (yValue, uValue, vValue) = RGBtoYUV(red, green, blue);

            for (var y = 0; y < input.Height; y++)
            {
                for (var x = 0; x < input.Width; x++)
                {
                    input.Y[x, y] = yValue;
                    input.U[x, y] = uValue;
                    input.V[x, y] = vValue;
                }
            }
        }

        private static (byte y, byte u, byte v) RGBtoYUV(byte r, byte g, byte b)
        {
            unchecked
            {
                var y = (byte)(((66 * r + 129 * g + 25 * b + 128) >> 8) + 16);
                var u = (byte)(((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128);
                var v = (byte)(((112 * r - 94 * g - 18 * b + 128) >> 8) + 128);

                return (y, u, v);
            }
        }

        private static void PictureOnMono(YUV input, int scale)
        {
            var resized = ResizeYUV(input, input.Width / scale, input.Height / scale);
            SolidFill(input, 128, 128, 128);
            PlaceAt(input, resized, new Point(-100, 0));
            PlaceAt(input, resized, new Point(input.Width / 2, input.Height / 2));
        }

        private static void PictureInPicture(YUV input, int scale)
        {
            var pictureWidth = input.Width / scale;
            var pictureHeight = input.Height / scale;
            var resized = ResizeYUV(input, pictureWidth, pictureHeight);
            PlaceAt(input, resized, new Point(-100, 0));
            PlaceAt(input, resized, new Point(input.Width / 2, input.Height / 2));
        }

        /*
        1111100000000000000000000000000000001111
        1111100000000000000000000000000000001111
        -----00000000000000000000000000000001111
        1111100000000000000000000000000000000000
        1111100000000000000000000000000000000000
        -----00000000000000000000000000000000000
        1111100000000000000000000000000000000000
        1111100000000000000000000000000000000000
        -----00000000000000000000000000000000000
        1111100000000000000000000000000000000000
        1111100000000000000000000000000000000000
         */
        private static void PresenterView(YUV input)
        {
            var audienceCount = 6;
            var borderWidth = input.Width / 20;

            var audience = ResizeYUV(input, input.Width / audienceCount, input.Height / audienceCount);
            for (var i = 0; i < audienceCount; i++)
                PlaceAt(input, audience, new Point(0, i * audience.Height));

            var presenterScale = 4;
            var presenter = ResizeYUV(input, input.Width / presenterScale, input.Height / presenterScale);
            PlaceAt(
                input,
                presenter,
                new Point(input.Width - presenter.Height - borderWidth, borderWidth),
                new Rectangle((presenter.Width - presenter.Height) / 2, 0, presenter.Height, presenter.Height));
        }

        private static YUV ResizeYUV(YUV original, int resizedWidth, int resizedHeight)
        {
            var resized = new YUV(resizedWidth, resizedHeight, original.Stride);

            var heightConversionRatio = original.Height * 1.0 / resizedHeight;
            var widthConversionRatio = original.Width * 1.0 / resizedWidth;

            SetY();
            SetUV();

            return resized;

            void SetY()
            {
                for (var y = 0; y < resizedHeight; y++)
                {
                    for (var x = 0; x < resizedWidth; x++)
                    {
                        var originalX = (int)(x * heightConversionRatio);
                        var originalY = (int)(y * widthConversionRatio);
                        try
                        {
                            resized.Y[x, y] = original.Y[originalX, originalY];
                        }
                        catch
                        {
                        }
                    }
                }
            }

            void SetUV()
            {
                for (var y = 0; y < resized.Height; y += 2)
                {
                    for (var x = 0; x < resized.Width; x += 2)
                    {
                        int originalX = (int)(x * widthConversionRatio);
                        int originalY = (int)(y * heightConversionRatio);
                        try
                        {
                            resized.U[x, y] = original.U[originalX, originalY];
                            resized.V[x, y] = original.V[originalX, originalY];
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        private static void PlaceAt(YUV input, YUV picture, Point offset, Rectangle? clip = null)
        {
            var clipRectangle = clip ?? new Rectangle(0, 0, picture.Width, picture.Height);
            clipRectangle = new Rectangle(
                Math.Min(Math.Max(0, clipRectangle.X), picture.Width),
                Math.Min(Math.Max(0, clipRectangle.Y), picture.Height),
                Math.Max(0, Math.Min(picture.Width, clipRectangle.Width)),
                Math.Max(0, Math.Min(picture.Height, clipRectangle.Height)));

            SetY();
            SetUV();

            void SetY()
            {
                for (var y = clipRectangle.Top; y < clipRectangle.Bottom; y++)
                {
                    for (var x = clipRectangle.Left; x < clipRectangle.Right; x++)
                    {
                        var targetX = x - clipRectangle.Left + offset.X;
                        var targetY = y - clipRectangle.Top + offset.Y;
                        if (targetX < 0 || targetX >= input.Width || targetY < 0 || targetY >= input.Height)
                            continue;
                        input.Y[targetX, targetY] = picture.Y[x, y];
                    }
                }
            }

            void SetUV()
            {
                for (var y = clipRectangle.Top; y < clipRectangle.Bottom; y += 2)
                {
                    for (var x = clipRectangle.Left; x < clipRectangle.Right; x += 2)
                    {
                        var targetX = x - clipRectangle.Left + offset.X ;
                        var targetY = y - clipRectangle.Top + offset.Y;
                        if (targetX < 0 || targetX >= input.Width || targetY < 0 || targetY >= input.Height)
                            continue;
                        input.U[targetX, targetY] = picture.U[x, y];
                        input.V[targetX, targetY] = picture.V[x, y];
                    }
                }
            }
        }
    }
}
