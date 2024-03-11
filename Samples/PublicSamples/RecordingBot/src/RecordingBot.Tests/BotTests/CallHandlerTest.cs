using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using RecordingBot.Model.Models;
using RecordingBot.Services.Bot;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Tests.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecordingBot.Tests.BotTests
{
    [TestFixture]
    public class CallHandlerTest
    {
        private AzureSettings _settings;

        private ICall _call;
        private IGraphLogger _logger;
        private ILocalMediaSession _mediaSession;
        
        private IEventPublisher _eventPublisher;

        [OneTimeSetUp]
        public void CallHandlerTestOneTimeSetup()
        {
            _settings = new AzureSettings
            {
                CaptureEvents = false,
                MediaFolder = "archive",
                EventsFolder = "events",
                IsStereo = false,
                AudioSettings = new Services.ServiceSetup.AudioSettings
                {
                    WavSettings = null
                },
            };

            _logger = Substitute.For<IGraphLogger>();
            _eventPublisher = Substitute.For<IEventPublisher>();

            _mediaSession = Substitute.For<ILocalMediaSession>();
            _mediaSession.AudioSocket.Returns(Substitute.For<IAudioSocket>());

            _call = Substitute.For<ICall>();
            _call.Participants.Returns(Substitute.For<IParticipantCollection>());
            _call.Resource.Returns(Substitute.For<Call>());
            _call.GraphLogger.Returns(_logger);
            _call.MediaSession.Returns(_mediaSession);

            _call.Resource.Source = new ParticipantInfo()
            {
                Identity = new IdentitySet()
                {
                    User = new Identity()
                    {
                        Id = new Guid().ToString()
                    }
                }
            };
        }

        [Test]
        public void TestOnParticipantUpdate()
        {
            var participantCount = 0;
            var handler = new CallHandler(_call, _settings, _eventPublisher);

            using (var fs = File.OpenRead(Path.Combine("TestData", "participants.zip")))
            {
                using (var zipInputStream = new ZipInputStream(fs))
                {
                    while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        ParticipantData data;

                        var temp = new byte[4096];
                        var ms = new MemoryStream();

                        StreamUtils.Copy(zipInputStream, ms, temp);

                        ms.Position = 0;

                        using (var bson = new BsonDataReader(ms))
                        {
                            JsonSerializer serializer = new();
                            serializer.Converters.Add(new ParticipantConverter());
                            data = serializer.Deserialize<ParticipantData>(bson);

                            Assert.That(data, Is.Not.Null);

                            object tryParseAsIdentity(KeyValuePair<string, object> pair)
                            {
                                try
                                {
                                    var identity = (Identity)serializer.Deserialize(new JTokenReader(pair.Value as JObject), typeof(Identity));
                                    return identity;
                                }
                                catch
                                {
                                    return pair.Value;
                                }
                            }

                            foreach (var resource in data.AddedResources)
                            {
                                if (resource.Resource.Info.Identity.AdditionalData != null)
                                {
                                    resource.Resource.Info.Identity.AdditionalData =
                                        resource.Resource.Info.Identity.AdditionalData
                                                                       .ToDictionary(additionalDataEntry => additionalDataEntry.Key,
                                                                                     tryParseAsIdentity);
                                }
                            }

                            var addedResourceWithUser = data.AddedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                            var addedResourceWithUserAndAdditionalData = data.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                            var addedResourceWithGuestUser = addedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                            var addedResourceWithNonGuestUser = addedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                            var addedResourceWithoutUserAndAdditionalData = data.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                            var removedResourceWithUser = data.RemovedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                            var removedResourceWithUserAndAdditionalData = data.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                            var removedResourceWithGuestUser = removedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                            var removedResourceWithNonGuestUser = removedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                            var removedResourceWithoutUserAndAdditionalData = data.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                            var c = new CollectionEventArgs<IParticipant>("", addedResources: data.AddedResources, updatedResources: null, removedResources: data.RemovedResources);
                            handler.ParticipantsOnUpdated(null, c);

                            var participants = handler.BotMediaStream.GetParticipants();

                            if (addedResourceWithUser.Count != 0)
                            {
                                var match = addedResourceWithUser.Count(participants.Contains);
                                Assert.That(match, Is.EqualTo(addedResourceWithUser.Count));
                            }

                            if (addedResourceWithGuestUser.Count != 0)
                            {
                                var match = participants
                                    .Where(x => x.Resource.Info.Identity.AdditionalData != null)
                                    .SelectMany(x => x.Resource.Info.Identity.AdditionalData)
                                    .Count(participantData => addedResourceWithGuestUser.Any(guest => guest.Value as Identity == participantData.Value as Identity));

                                Assert.That(match, Is.EqualTo(addedResourceWithGuestUser.Count));
                            }

                            participantCount += addedResourceWithUser.Count + addedResourceWithGuestUser.Count;
                            participantCount -= removedResourceWithUser.Count + removedResourceWithGuestUser.Count;

                            Assert.That(participants.Count, Is.EqualTo(participantCount));
                        }
                    }
                }
            }
        }
    }
}
