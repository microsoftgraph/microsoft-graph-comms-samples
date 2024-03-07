// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="CallHandlerTest.cs" company="Microsoft">
//     Copyright © 2020
// </copyright>
// <summary></summary>
// ***********************************************************************
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
    /// <summary>
    /// Defines test class CallHandlerTest.
    /// </summary>
    [TestFixture]
    public class CallHandlerTest
    {
        /// <summary>
        /// The settings
        /// </summary>
        private AzureSettings _settings;
        /// <summary>
        /// The call
        /// </summary>
        private ICall _call;
        /// <summary>
        /// The logger
        /// </summary>
        private IGraphLogger _logger;
        /// <summary>
        /// The media session
        /// </summary>
        private ILocalMediaSession _mediaSession;
        /// <summary>
        /// The event publisher
        /// </summary>
        private IEventPublisher _eventPublisher;

        /// <summary>
        /// Calls the handler test one time setup.
        /// </summary>
        [OneTimeSetUp]
        public void CallHandlerTestOneTimeSetup()
        {
            // Load() will automatically look for a .env file in the current directory
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

        /// <summary>
        /// Defines the test method TestOnParticipantUpdate.
        /// </summary>
        [Test]
        public void TestOnParticipantUpdate()
        {
            var participantCount = 0;
            var handler = new CallHandler(_call, _settings, _eventPublisher);

            using (var fs = System.IO.File.OpenRead(Path.Combine("TestData", "participants.zip")))
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

                            _ = data.AddedResources.Select(x =>
                            {
                                if (x.Resource.Info.Identity.AdditionalData != null)
                                {
                                    var d = x.Resource.Info.Identity.AdditionalData;
                                    var ad = new Dictionary<string, object>();
                                    d.ForEach(y =>
                                    {
                                        try
                                        {
                                            var i = (Identity)serializer.Deserialize(new JTokenReader(y.Value as JObject), typeof(Identity));
                                            ad.Add(y.Key, i);
                                        }
                                        catch
                                        {
                                            ad.Add(y.Key, y.Value);
                                        }
                                    });
                                    x.Resource.Info.Identity.AdditionalData = ad;
                                }
                                return x;
                            }).ToList();

                            Assert.That(data, Is.Not.Null);

                            var addKnownUser = data.AddedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                            var addAdditionalDataUser = data.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                            var addGuestUser = addAdditionalDataUser.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                            var addGuestNonUser = addAdditionalDataUser.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                            var addUnkownUser = data.AddedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                            var removeKnownUser = data.RemovedResources.Where(x => x.Resource.Info.Identity.User != null).ToList();
                            var removeAdditionalDataUser = data.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData != null).ToList();
                            var removeGuestUser = removeAdditionalDataUser.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key != "applicationInstance" && x.Value is Identity).ToList();
                            var removeGuestNonUser = removeAdditionalDataUser.SelectMany(x => x.Resource.Info.Identity.AdditionalData).Where(x => x.Key == "applicationInstance" || x.Value is not Identity).ToList();
                            var removeUnkownUser = data.RemovedResources.Where(x => x.Resource.Info.Identity.User == null && x.Resource.Info.Identity.AdditionalData == null).ToList();

                            var c = new CollectionEventArgs<IParticipant>("", addedResources: data.AddedResources, updatedResources: null, removedResources: data.RemovedResources);
                            handler.ParticipantsOnUpdated(null, c);

                            var participants = handler.BotMediaStream.GetParticipants();

                            if (addKnownUser.Count != 0)
                            {
                                var match = addKnownUser.Where(x => participants.Contains(x)).Count();
                                Assert.That(match, Is.EqualTo(addKnownUser.Count));
                            }

                            if (addGuestUser.Count != 0)
                            {
                                var match = participants
                                    .Where(x => x.Resource.Info.Identity.AdditionalData != null)
                                    .SelectMany(x => x.Resource.Info.Identity.AdditionalData)
                                    .ToList()
                                    .Where(x =>
                                        addGuestUser
                                        .Where(y => y.Value as Identity == x.Value as Identity).Any())
                                    .Count();

                                Assert.That(match, Is.EqualTo(addGuestUser.Count));
                            }

                            participantCount += addKnownUser.Count + addGuestUser.Count;
                            participantCount -= removeKnownUser.Count + removeGuestUser.Count;

                            Assert.That(participants.Count, Is.EqualTo(participantCount));
                        }
                    }
                }
            }
        }
    }
}
