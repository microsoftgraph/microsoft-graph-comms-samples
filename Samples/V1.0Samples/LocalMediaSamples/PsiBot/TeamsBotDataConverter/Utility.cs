// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Psi store utility methods.
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// Encode image streams, generating a new store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="output">Output store name.</param>
        /// <param name="quality">Start time relative to beginning.</param>
        /// <returns>Success flag.</returns>
        internal static int EncodeImageStreams(string store, string path, string output, int quality)
        {
            Console.WriteLine($"Encoding image streams (store={store}, path={path}, output={output}, quality={quality}");

            bool IsPlainImageStream(IStreamMetadata streamInfo)
            {
                return Type.GetType(streamInfo.TypeName, false) == typeof(Shared<Image>);
            }

            bool IsDictionaryOfImageStream(IStreamMetadata streamInfo)
            {
                return Type.GetType(streamInfo.TypeName, false) == typeof(Dictionary<string, Shared<Image>>);
            }

            bool IsImageStream(IStreamMetadata streamInfo)
            {
                return IsPlainImageStream(streamInfo) || IsDictionaryOfImageStream(streamInfo);
            }

            void Encode(IStreamMetadata streamInfo, PsiImporter importer, Exporter exporter)
            {
                if (IsPlainImageStream(streamInfo))
                {
                    Console.WriteLine($"Encoding stream: {streamInfo.Name}");
                    importer
                        .OpenStream<Shared<Image>>(streamInfo.Name)
                        .EncodeJpeg(quality)
                        .Write(streamInfo.Name, exporter, true);
                }
                else if (IsDictionaryOfImageStream(streamInfo))
                {
                    Console.WriteLine($"Encoding stream: {streamInfo.Name}");
                    importer
                        .OpenStream<Dictionary<string, Shared<Image>>>(streamInfo.Name)
                        .Select(dict => dict.Select(kv =>
                        {
                            var img = kv.Value.Resource;
                            var shared = EncodedImagePool.GetOrCreate(img.Width, img.Height, PixelFormat.BGR_24bpp);
                            shared.Resource.EncodeFrom(img, new ImageToJpegStreamEncoder { QualityLevel = quality });
                            return (kv.Key, shared);
                        }).ToDictionary(x => x.Key, x => x.shared))
                        .Write(streamInfo.Name, exporter, true);
                }
            }

            PsiStore.Process(IsImageStream, Encode, (store, path), (output, path), true, new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")), Console.WriteLine);

            return 0;
        }

        /// <summary>
        /// Encode image streams, generating a new store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="output">Output store name.</param>
        /// <param name="quality">Start time relative to beginning.</param>
        /// <returns>Success flag.</returns>
        internal static int SplitDictionaryStreams(string store, string path, string output, int quality)
        {
            Console.WriteLine($"Splitting dictionary streams (store={store}, path={path}, output={output})");

            bool IsDictionaryOfEncodedImageStream(IStreamMetadata streamInfo)
            {
                return Type.GetType(streamInfo.TypeName, false) == typeof(Dictionary<string, Shared<EncodedImage>>);
            }

            bool IsDictionaryOfImageStream(IStreamMetadata streamInfo)
            {
                return Type.GetType(streamInfo.TypeName, false) == typeof(Dictionary<string, Shared<Image>>);
            }

            bool IsDictionaryOfAudioBufferStream(IStreamMetadata streamInfo)
            {
                return Type.GetType(streamInfo.TypeName, false) == typeof(Dictionary<string, AudioBuffer>);
            }

            bool IsDictionaryStream(IStreamMetadata streamInfo)
            {
                return IsDictionaryOfEncodedImageStream(streamInfo) || IsDictionaryOfImageStream(streamInfo) || IsDictionaryOfAudioBufferStream(streamInfo);
            }

            void Split(IStreamMetadata streamInfo, PsiImporter importer, Exporter exporter)
            {
                if (IsDictionaryOfEncodedImageStream(streamInfo))
                {
                    // already encoded
                    Console.WriteLine($"Splitting stream: {streamInfo.Name}");
                    importer
                        .OpenStream<Dictionary<string, Shared<EncodedImage>>>(streamInfo.Name)
                        .Parallel(
                            (id, video) => video.Write($"ParticipantVideo_{id}", exporter, true),
                            branchTerminationPolicy: (id, vid, dt) => (false, DateTime.MaxValue));
                }
                else if (IsDictionaryOfImageStream(streamInfo))
                {
                    // not yet encoded
                    Console.WriteLine($"Splitting stream: {streamInfo.Name}");
                    importer
                        .OpenStream<Dictionary<string, Shared<Image>>>(streamInfo.Name)
                        .Parallel(
                            (id, video) => video.EncodeJpeg(quality).Write($"ParticipantVideo_{id}", exporter, true),
                            branchTerminationPolicy: (id, vid, dt) => (false, DateTime.MaxValue));
                }
                else if (IsDictionaryOfAudioBufferStream(streamInfo))
                {
                    Console.WriteLine($"Splitting stream: {streamInfo.Name}");
                    importer
                        .OpenStream<Dictionary<string, AudioBuffer>>(streamInfo.Name)
                        .Parallel(
                            (id, audio) => audio.Write($"ParticipantAudio_{id}", exporter),
                            branchTerminationPolicy: (id, vid, dt) => (false, DateTime.MaxValue));
                }
            }

            PsiStore.Process(IsDictionaryStream, Split, (store, path), (output, path), true, new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")), Console.WriteLine);

            return 0;
        }
    }
}