// <copyright file="MediaFileWriter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Media file writer.
    /// </summary>
    public class MediaFileWriter : IDisposable
    {
        /// <summary>
        /// object.
        /// </summary>
        private readonly object @lock = new object();
        private readonly object writelock = new object();
        private Stream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFileWriter"/> class.
        /// </summary>
        /// <param name="stream">stream.</param>
        public MediaFileWriter(Stream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Event callback to Flush/FlushFinalBlock for stream on before disposing stream object.
        /// </summary>
        public event EventHandler<Stream> OnDisposing;

        /// <summary>
        /// Gets or sets Timestamp of the first buffer.
        /// </summary>
        public long StreamStarted { get; protected set; }

        /// <summary>
        /// Gets or sets Timestamp of the current (most recently received) buffer.
        /// </summary>
        public long StreamCurrent { get; protected set; }

        /// <summary>
        /// Gets or sets Stream id (MSI) of the current (most recently received) buffer.
        /// </summary>
        public uint CurrentStreamId { get; set; } = uint.MaxValue;

        /// <summary>
        /// Gets or sets the value of file name.
        /// </summary>
        public string Filename { get; protected set; }

        /// <summary>
        /// Write stream to file.
        /// </summary>
        /// <param name="timestamp">timestamp.</param>
        /// <param name="mediaBufferData">media buffer data.</param>
        /// <param name="mediaBufferLength">media buffer lenghth.</param>
        public virtual void Write(long timestamp, IntPtr mediaBufferData, long mediaBufferLength)
        {
            lock (this.writelock)
            {
                unsafe
                {
                    using (var uStream = new UnmanagedMemoryStream((byte*)mediaBufferData, mediaBufferLength))
                    {
                        uStream.CopyTo(this.stream);
                    }
                }
            }
        }

        /// <summary>
        /// Write stream to file.
        /// </summary>
        /// <param name="timestamp">Timestamp.</param>
        /// <param name="data">Media buffer data.</param>
        /// <returns>returns task.</returns>
        public async Task WriteAsync(long timestamp, byte[] data)
        {
            if (this.StreamStarted == 0)
            {
                this.StreamStarted = timestamp;
            }

            this.StreamCurrent = timestamp;

            await this.stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        /// <summary>
        /// dispose.
        /// </summary>
        public virtual void Dispose()
        {
            lock (this.@lock)
            {
                this.OnDisposing?.Invoke(this, this.stream);
#pragma warning disable IDISP007 // Don't dispose injected.
                this.stream?.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected.
                this.stream = null;
            }
        }
    }
}
