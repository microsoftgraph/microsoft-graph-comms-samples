// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MediaUtils.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the CallHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.HueBot.Bot
{
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using Microsoft.Graph.Communications.Common.Telemetry;

    /// <summary>
    /// The media utils class.
    /// </summary>
    public class MediaUtils
    {
        /// <summary>
        /// Transform NV12 to bmp image so we can view how is it looks like. Note it's not NV12 to RBG conversion.
        /// </summary>
        /// <param name="data">NV12 sample data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="logger">Log instance.</param>
        /// <returns>The <see cref="Bitmap"/>.</returns>
        public static Bitmap TransformNv12ToBmpFaster(byte[] data, int width, int height, IGraphLogger logger)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppRgb);

            var uvStart = width * height;
            for (var y = 0; y < height; y++)
            {
                var pos = y * width;
                var posInBmp = y * bmpData.Stride;
                for (var x = 0; x < width; x++)
                {
                    var vIndex = uvStart + ((y >> 1) * width) + (x & ~1);

                    //// https://msdn.microsoft.com/en-us/library/windows/desktop/dd206750(v=vs.85).aspx
                    //// https://en.wikipedia.org/wiki/YUV
                    var c = data[pos] - 16;
                    var d = data[vIndex] - 128;
                    var e = data[vIndex + 1] - 128;
                    c = c < 0 ? 0 : c;

                    var r = ((298 * c) + (409 * e) + 128) >> 8;
                    var g = ((298 * c) - (100 * d) - (208 * e) + 128) >> 8;
                    var b = ((298 * c) + (516 * d) + 128) >> 8;
                    r = r.Clamp(0, 255);
                    g = g.Clamp(0, 255);
                    b = b.Clamp(0, 255);

                    Marshal.WriteInt32(bmpData.Scan0, posInBmp + (x << 2), (b << 0) | (g << 8) | (r << 16) | (0xFF << 24));
                    pos++;
                }
            }

            bmp.UnlockBits(bmpData);

            watch.Stop();
            logger.Info($"Took {watch.ElapsedMilliseconds} ms to lock and unlock");

            return bmp;
        }
    }
}