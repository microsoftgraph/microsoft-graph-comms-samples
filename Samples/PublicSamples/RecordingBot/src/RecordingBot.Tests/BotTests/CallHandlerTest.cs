using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.OData;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Models;
using RecordingBot.Services.Bot;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Tests.Helper;
using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.Preserve,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            jsonSerializerOptions.Converters.Add(new ODataJsonConverterFactory(null, null, typeAssemblies: [.. SerializerAssemblies.Assemblies, typeof(SerializableParticipantEvent).Assembly]));
            jsonSerializerOptions.Converters.Add(new TypeMappingConverter<IParticipant, SerilizableParticipant>());

            var participantCount = 0;
            var handler = new CallHandler(_call, _settings, _eventPublisher);

            using (var archive = new ZipFile(Path.Combine("TestData", "participants.zip")))
            {
                foreach (ZipEntry file in archive)
                {
                    using (var fileStream = archive.GetInputStream(file))
                    {
                        var json = ((JObject)JToken.ReadFrom(new BsonDataReader(fileStream))).ToString();
                        var deserialized = JsonSerializer.Deserialize<SerializableParticipantEvent>(json, jsonSerializerOptions);

                        Assert.That(deserialized, Is.Not.Null);

                        var addedResourceWithUser = deserialized.AddedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                        var addedResourceWithUserAndAdditionalData = deserialized.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                        var addedResourceWithGuestUser = addedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                        var addedResourceWithNonGuestUser = addedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                        var addedResourceWithoutUserAndAdditionalData = deserialized.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                        var removedResourceWithUser = deserialized.RemovedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                        var removedResourceWithUserAndAdditionalData = deserialized.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                        var removedResourceWithGuestUser = removedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                        var removedResourceWithNonGuestUser = removedResourceWithUserAndAdditionalData.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                        var removedResourceWithoutUserAndAdditionalData = deserialized.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                        var c = new CollectionEventArgs<IParticipant>("", addedResources: deserialized.AddedResources, updatedResources: null, removedResources: deserialized.RemovedResources);
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
