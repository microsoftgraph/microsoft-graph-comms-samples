using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Graph.Communications.Common.Telemetry;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NSubstitute;
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
    [TestFixture]
    public class MediaStreamTest
    {
        private MediaStream _mediaStream;
        private AzureSettings _settings;

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

            var logger = Substitute.For<IGraphLogger>();
            _mediaStream = new MediaStream(_settings, logger, Guid.NewGuid().ToString());
        }

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
                            var p = DeserializeParticipant.GetParticipant(e);

                            Assert.That(d.IsSilence, Is.EqualTo(e.IsSilence));
                            Assert.That(d.Length, Is.EqualTo(e.Length));
                            Assert.That(d.Timestamp, Is.EqualTo(e.Timestamp));
                            Assert.That(d.ActiveSpeakers, Is.EqualTo(e.ActiveSpeakers));
                            Assert.That(d.UnmixedAudioBuffers?.Length, Is.EqualTo(e.SerializableUnmixedAudioBuffers?.Length));

                            Assert.That((d.Data != IntPtr.Zero && e.Buffer != null) || (d.Data == IntPtr.Zero && e.Buffer == null));

                            if (d.Data != IntPtr.Zero && e.Buffer != null)
                            {
                                var buffer = new byte[d.Length];
                                Marshal.Copy(d.Data, buffer, 0, (int)d.Length);

                                Assert.That(buffer, Is.EqualTo(e.Buffer));
                            }

                            for (int i = 0; i < e.SerializableUnmixedAudioBuffers?.Length; i++)
                            {
                                Assert.That(d.UnmixedAudioBuffers.Length, Is.EqualTo(e.SerializableUnmixedAudioBuffers.Length));
                                Assert.That(p.Count, Is.EqualTo(e.SerializableUnmixedAudioBuffers.Length));

                                var source = e.SerializableUnmixedAudioBuffers[i];
                                var actual = d.UnmixedAudioBuffers[i];
                                var participant = p[i].Resource.Info.Identity.User;

                                Assert.That(actual.ActiveSpeakerId, Is.EqualTo(source.ActiveSpeakerId));
                                Assert.That(actual.Length, Is.EqualTo(source.Length));
                                Assert.That(actual.OriginalSenderTimestamp, Is.EqualTo(source.OriginalSenderTimestamp));

                                var buffer = new byte[actual.Length];
                                Marshal.Copy(actual.Data, buffer, 0, (int)actual.Length);

                                Assert.That(buffer, Is.EqualTo(source.Buffer));

                                Assert.That(participant.DisplayName, Is.EqualTo(source.DisplayName));
                                Assert.That(participant.AdditionalData, Is.EqualTo(source.AdditionalData));
                                Assert.That(participant.Id, Is.EqualTo(source.AdId));
                                Assert.That(p[i].Resource.MediaStreams.Any(x => x.SourceId == source.ActiveSpeakerId.ToString()));
                                Assert.That(p[i].Resource.IsInLobby, Is.False);
                            }

                            await _mediaStream.AppendAudioBuffer(d, p);
                        }
                    }
                }
            }

            await _mediaStream.End();
        }

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
                    var p = DeserializeParticipant.GetParticipant(e);

                    Assert.That((d.Data != IntPtr.Zero && e.Buffer != null) || (d.Data == IntPtr.Zero && e.Buffer == null));

                    Assert.That(d.IsSilence, Is.EqualTo(e.IsSilence));
                    Assert.That(d.Length, Is.EqualTo(e.Length));
                    Assert.That(d.Timestamp, Is.EqualTo(e.Timestamp));
                    Assert.That(d.ActiveSpeakers, Is.EqualTo(e.ActiveSpeakers));
                    Assert.That(d.UnmixedAudioBuffers?.Length, Is.EqualTo(e.SerializableUnmixedAudioBuffers?.Length));

                    if (d.Data != IntPtr.Zero && e.Buffer != null)
                    {
                        var buffer = new byte[d.Length];
                        Marshal.Copy(d.Data, buffer, 0, (int)d.Length);

                        Assert.That(buffer, Is.EqualTo(e.Buffer));
                    }

                    for (int i = 0; i < e.SerializableUnmixedAudioBuffers?.Length; i++)
                    {
                        Assert.That(d.UnmixedAudioBuffers.Length, Is.EqualTo(e.SerializableUnmixedAudioBuffers.Length));
                        Assert.That(p.Count, Is.EqualTo(e.SerializableUnmixedAudioBuffers.Length));

                        var source = e.SerializableUnmixedAudioBuffers[i];
                        var actual = d.UnmixedAudioBuffers[i];
                        var participant = p[i].Resource.Info.Identity.User;

                        Assert.That(actual.ActiveSpeakerId, Is.EqualTo(source.ActiveSpeakerId));
                        Assert.That(actual.Length, Is.EqualTo(source.Length));
                        Assert.That(actual.OriginalSenderTimestamp, Is.EqualTo(source.OriginalSenderTimestamp));

                        var buffer = new byte[actual.Length];
                        Marshal.Copy(actual.Data, buffer, 0, (int)actual.Length);

                        Assert.That(buffer, Is.EqualTo(source.Buffer));

                        Assert.That(participant.DisplayName, Is.EqualTo(source.DisplayName));
                        Assert.That(participant.AdditionalData, Is.EqualTo(source.AdditionalData));
                        Assert.That(participant.Id, Is.EqualTo(source.AdId));
                        Assert.That(p[i].Resource.MediaStreams.Any(x => x.SourceId == source.ActiveSpeakerId.ToString()));
                        Assert.That(p[i].Resource.IsInLobby, Is.False);

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

            var fileNames = GetfileNames(lastZip).ToList();
            foreach (var userId in userIds)
            {
                var match = fileNames.FirstOrDefault(_ => _.Contains(userId));
                Assert.That(match, Is.Not.Null);
            }

            lastFileInfo.Directory?.Delete(true);
        }

        static IEnumerable<string> GetfileNames(string zipFile)
        {
            foreach (var file in ZipUtils.GetEntries(zipFile))
            {
                yield return file.Item1.Name;
            }
        }
    }
}
