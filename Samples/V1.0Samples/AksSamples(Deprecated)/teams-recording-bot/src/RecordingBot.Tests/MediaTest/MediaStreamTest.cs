// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="MediaStreamTest.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Graph.Communications.Common.Telemetry;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NUnit.Framework;
using RecordingBot.Services.Media;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using RecordingBot.Tests.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RecordingBot.Tests.MediaTest
{
    /// <summary>
    /// Defines test class MediaStreamTest.
    /// Implements the <see cref="RecordingBot.Tests.TestBase" />
    /// </summary>
    /// <seealso cref="RecordingBot.Tests.TestBase" />
    [TestFixture]
    public class MediaStreamTest : TestBase
    {
        /// <summary>
        /// The media stream
        /// </summary>
        private MediaStream _mediaStream;
        /// <summary>
        /// The settings
        /// </summary>
        private AzureSettings _settings;

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _settings = new AzureSettings
            {
                CaptureEvents = false,
                MediaFolder = "archive",
                IsStereo = false,
                AudioSettings = new AudioSettings
                {
                    WavSettings = null
                },
            };

            var logger = new Mock<IGraphLogger>();
            _mediaStream = new MediaStream(_settings, logger.Object, Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Defines the test method TestReplayStreamContentMatches.
        /// </summary>
        [Test]
        public async Task TestReplayStreamContentMatches()
        {
            using (var fs = File.OpenRead(Path.Combine("TestData", "recording.zip")))
            {
                using (var zipInputStream = new ZipInputStream(fs))
                {
                    while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        var temp = new byte[4096];
                        var ms = new MemoryStream();

                        StreamUtils.Copy(zipInputStream, ms, temp);

                        ms.Position = 0;

                        using (var bson = new BsonDataReader(ms))
                        {
                            var e = new JsonSerializer().Deserialize<SerializableAudioMediaBuffer>(bson);
                            var d = new DeserializeAudioMediaBuffer(e);
                            var p = new DeserializeParticipant().GetParticipant(e);

                            Assert.AreEqual(e.IsSilence, d.IsSilence);
                            Assert.AreEqual(e.Length, d.Length);
                            Assert.AreEqual(e.Timestamp, d.Timestamp);
                            Assert.AreEqual(e.ActiveSpeakers, d.ActiveSpeakers);
                            Assert.AreEqual(e.SerializableUnmixedAudioBuffers?.Length, d.UnmixedAudioBuffers?.Length);

                            Assert.That((d.Data != IntPtr.Zero && e.Buffer != null) || (d.Data == IntPtr.Zero && e.Buffer == null));

                            if (d.Data != IntPtr.Zero && e.Buffer != null)
                            {
                                var buffer = new byte[d.Length];
                                Marshal.Copy(d.Data, buffer, 0, (int)d.Length);

                                Assert.AreEqual(e.Buffer, buffer);
                            }

                            for (int i = 0; i < e.SerializableUnmixedAudioBuffers?.Length; i++)
                            {
                                Assert.AreEqual(e.SerializableUnmixedAudioBuffers.Length, d.UnmixedAudioBuffers.Length);
                                Assert.AreEqual(e.SerializableUnmixedAudioBuffers.Length, p.Count);

                                var source = e.SerializableUnmixedAudioBuffers[i];
                                var actual = d.UnmixedAudioBuffers[i];
                                var participant = p[i].Resource.Info.Identity.User;

                                Assert.AreEqual(source.ActiveSpeakerId, actual.ActiveSpeakerId);
                                Assert.AreEqual(source.Length, actual.Length);
                                Assert.AreEqual(source.OriginalSenderTimestamp, actual.OriginalSenderTimestamp);

                                var buffer = new byte[actual.Length];
                                Marshal.Copy(actual.Data, buffer, 0, (int)actual.Length);

                                Assert.AreEqual(source.Buffer, buffer);

                                Assert.AreEqual(source.DisplayName, participant.DisplayName);
                                Assert.AreEqual(source.AdditionalData, participant.AdditionalData);
                                Assert.AreEqual(source.AdId, participant.Id);
                                Assert.That(p[i].Resource.MediaStreams.Any(x => x.SourceId == source.ActiveSpeakerId.ToString()));
                                Assert.IsFalse(p[i].Resource.IsInLobby);
                            }

                            await _mediaStream.AppendAudioBuffer(d, p);
                        }
                    }
                }
            }

            await _mediaStream.End();
        }

        /// <summary>
        /// Defines the test method TestPrepareZip.
        /// </summary>
        [Test]
        public async Task TestPrepareZip()
        {
            var userIds = new List<string>();
            var currentAudioProcessor = new AudioProcessor(_settings);

            foreach (var (zipEntry, zipInputStream) in ZipUtils.GetEntries(Path.Combine("TestData", "recording.zip")))
            {
                var temp = new byte[4096];
                var ms = new MemoryStream();

                StreamUtils.Copy(zipInputStream, ms, temp);

                ms.Position = 0;

                using (var bson = new BsonDataReader(ms))
                {
                    var e = new JsonSerializer().Deserialize<SerializableAudioMediaBuffer>(bson);
                    var d = new DeserializeAudioMediaBuffer(e);
                    var p = new DeserializeParticipant().GetParticipant(e);

                    Assert.That((d.Data != IntPtr.Zero && e.Buffer != null) || (d.Data == IntPtr.Zero && e.Buffer == null));

                    Assert.AreEqual(e.IsSilence, d.IsSilence);
                    Assert.AreEqual(e.Length, d.Length);
                    Assert.AreEqual(e.Timestamp, d.Timestamp);
                    Assert.AreEqual(e.ActiveSpeakers, d.ActiveSpeakers);
                    Assert.AreEqual(e.SerializableUnmixedAudioBuffers?.Length, d.UnmixedAudioBuffers?.Length);

                    if (d.Data != IntPtr.Zero && e.Buffer != null)
                    {
                        var buffer = new byte[d.Length];
                        Marshal.Copy(d.Data, buffer, 0, (int)d.Length);

                        Assert.AreEqual(e.Buffer, buffer);
                    }

                    for (int i = 0; i < e.SerializableUnmixedAudioBuffers?.Length; i++)
                    {
                        Assert.AreEqual(e.SerializableUnmixedAudioBuffers.Length, d.UnmixedAudioBuffers.Length);
                        Assert.AreEqual(e.SerializableUnmixedAudioBuffers.Length, p.Count);

                        var source = e.SerializableUnmixedAudioBuffers[i];
                        var actual = d.UnmixedAudioBuffers[i];
                        var participant = p[i].Resource.Info.Identity.User;

                        Assert.AreEqual(source.ActiveSpeakerId, actual.ActiveSpeakerId);
                        Assert.AreEqual(source.Length, actual.Length);
                        Assert.AreEqual(source.OriginalSenderTimestamp, actual.OriginalSenderTimestamp);

                        var buffer = new byte[actual.Length];
                        Marshal.Copy(actual.Data, buffer, 0, (int)actual.Length);

                        Assert.AreEqual(source.Buffer, buffer);

                        Assert.AreEqual(source.DisplayName, participant.DisplayName);
                        Assert.AreEqual(source.AdditionalData, participant.AdditionalData);
                        Assert.AreEqual(source.AdId, participant.Id);
                        Assert.That(p[i].Resource.MediaStreams.Any(x => x.SourceId == source.ActiveSpeakerId.ToString()));
                        Assert.IsFalse(p[i].Resource.IsInLobby);

                        if (!userIds.Contains(source.AdId) && source.AdId != null)
                        {
                            userIds.Add(source.AdId);
                        }
                    }

                    await currentAudioProcessor.Append(e);
                }
            }

            var lastZip = await currentAudioProcessor.Finalise();
            var lastFileInfo = new FileInfo(lastZip);

            var fileNames = getfileNames(lastZip).ToList();
            foreach (var userId in userIds)
            {
                var match = fileNames.FirstOrDefault(_ => _.Contains(userId));
                Assert.NotNull(match);
            }

            lastFileInfo.Directory?.Delete(true);
        }

        /// <summary>
        /// Getfiles the names.
        /// </summary>
        /// <param name="zipFile">The zip file.</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> getfileNames(string zipFile)
        {
            foreach (var file in ZipUtils.GetEntries(zipFile))
            {
                yield return file.Item1.Name;
            }
        }
    }
}
