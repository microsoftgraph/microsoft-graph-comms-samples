// <copyright file="BotService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using PsiBot.Model.Models;
using PsiBot.Services.Authentication;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using PsiBot.Service.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using PsiBot.Model.Constants;
using System.Linq;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Class BotService.
    /// Implements the <see cref="System.IDisposable" />
    /// Implements the <see cref="PsiBot.Services.Contract.IBotService" />
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="PsiBot.Services.Contract.IBotService" />
    public class BotService : IDisposable, IBotService
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        /// <summary>
        /// The bot configuration.
        /// </summary>
        private readonly BotConfiguration botConfiguration;

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        public ICommunicationsClient Client { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Client?.Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotService" /> class.

        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="settings">The bot configuration.</param>
        public BotService(
            IGraphLogger logger,
            IOptions<BotConfiguration> botConfiguration

        )
        {
            _logger = logger;
            this.botConfiguration = botConfiguration.Value;
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        public void Initialize()
        {
            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                botConfiguration.AadAppId,
                _logger);

            var authProvider = new AuthenticationProvider(
                name,
                botConfiguration.AadAppId,
                botConfiguration.AadAppSecret,
                _logger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(botConfiguration.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(botConfiguration.MediaPlatformSettings);
            builder.SetServiceBaseUrl(botConfiguration.PlaceCallEndpointUrl);

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        public async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall _);
            }
        }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallBody joinCallBody)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = Guid.NewGuid();

            var (chatInfo, meetingInfo) = ParseJoinURL(joinCallBody.JoinURL);

            // A short join URL resolves to JoinMeetingIdMeetingInfo, which carries no organizer, so
            // the tenant cannot be derived from the URL. Fall back to the tenant on the request body.
            var tenantId = (meetingInfo as OrganizerMeetingInfo)?.Organizer?.GetPrimaryIdentity()?.GetTenantId()
                ?? joinCallBody.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException(
                    "TenantId is required when joining with a short meeting URL, because the URL does not carry the organizer.",
                    nameof(joinCallBody));
            }

            var mediaSession = this.CreateLocalMediaSession();

            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession)
            {
                TenantId = tenantId,
            };

            if (!string.IsNullOrWhiteSpace(joinCallBody.DisplayName))
            {
                // Teams client does not allow changing of ones own display name.
                // If display name is specified, we join as anonymous (guest) user
                // with the specified display name.  This will put bot into lobby
                // unless lobby bypass is disabled.
                joinParams.GuestIdentity = new Identity
                {
                    Id = Guid.NewGuid().ToString(),
                    DisplayName = joinCallBody.DisplayName,
                };
            }

            var statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
            statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
            return statefulCall;
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">The media session identifier.
        /// This should be a unique value for each call.</param>
        /// <returns>The <see cref="ILocalMediaSession" />.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                var videoSocketSettings = new List<VideoSocketSettings>
                {
                    // add the main video socket sendrecv capable
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,

                        // We loop back the video in this sample. The MediaPlatform always sends only NV12 frames.
                        // So include only NV12 video in supportedSendVideoFormats
                        ReceiveColorFormat = VideoColorFormat.NV12,
                        SupportedSendVideoFormats = BotMediaStream.VideoFormatMap.Values.OfType<VideoFormat>().ToList(),
                        MaxConcurrentSendStreams = 1,
                    },
                };

                // create the receive only sockets settings for the multiview support
                for (int i = 0; i < BotConstants.NumberOfMultiviewSockets; i++)
                {
                    videoSocketSettings.Add(new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Recvonly,
                        ReceiveColorFormat = VideoColorFormat.NV12,
                    });
                }

                // Create the VBSS socket settings
                var vbssSocketSettings = new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.NV12,
                    MediaType = MediaType.Vbss,
                    SupportedSendVideoFormats = new List<VideoFormat>
                    {
                        VideoFormat.NV12_1920x1080_15Fps,
                    },
                };

                // create media session object, this is needed to establish call connections
                return this.Client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = true //get the extra buffers for the speakers
                    },
                    videoSocketSettings,
                    vbssSocketSettings,
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception e)
            {
                _logger.Log(System.Diagnostics.TraceLevel.Error, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TResource}" /> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // The context associated with the incoming call.
                IncomingContext incomingContext =
                    call.Resource.IncomingContext;

                // The RP participant.
                string observedParticipantId =
                    incomingContext.ObservedParticipantId;

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity =
                    incomingContext.OnBehalfOf;

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity =
                    incomingContext.Transferor;

                string countryCode = null;
                EndpointType? endpointType = null;

                // Note: this should always be true for CR calls.
                if (incomingContext.ObservedParticipantId == incomingContext.SourceParticipantId)
                {
                    // The dynamic location of the RP.
                    countryCode = call.Resource.Source.CountryCode;

                    // The type of endpoint being used.
                    endpointType = call.Resource.Source.EndpointType;
                }

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection" /> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}" /> instance containing the event data.</param>
        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(call, botConfiguration);
                this.CallHandlers[call.Id] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="CallHandler" />.</returns>
        /// <exception cref="ArgumentException">call ({callLegId}) not found</exception>
        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!this.CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ArgumentException($"call ({callLegId}) not found");
            }

            return handler;
        }

        /// <summary>
        /// Parse Join URL into its components.
        /// </summary>
        /// <param name="joinURL">Join URL from Team's meeting body.</param>
        /// <returns>Parsed data.</returns>
        /// <exception cref="ArgumentException">Join URL cannot be null or empty: {joinURL} - joinURL</exception>
        /// <exception cref="ArgumentException">Join URL cannot be parsed: {joinURL} - joinURL</exception>
        /// <exception cref="ArgumentException">Join URL is invalid: missing Tid - joinURL</exception>
        private (ChatInfo, MeetingInfo) ParseJoinURL(string joinURL)
        {
            if (string.IsNullOrEmpty(joinURL))
            {
                throw new ArgumentException($"Join URL cannot be null or empty: {joinURL}", nameof(joinURL));
            }

            var decodedURL = WebUtility.UrlDecode(joinURL);

            //// URL being needs to be in this format.
            //// https://teams.microsoft.com/l/meetup-join/19:cd9ce3da56624fe69c9d7cd026f9126d@thread.skype/1509579179399?context={"Tid":"72f988bf-86f1-41af-91ab-2d7cd011db47","Oid":"550fae72-d251-43ec-868c-373732c2704f","MessageId":"1536978844957"}

            //// Short join URL form:
            //// https://<host>/meet/<joinMeetingId>?p=<passcode>
            ////
            //// The host varies by cloud, so match on the
            //// "/meet/<id>" path shape rather than on a specific domain. Parsed from the raw URL
            //// rather than decodedURL, because UrlDecode turns "+" into a space and "%26" into
            //// "&", either of which would corrupt the passcode.
            if (TryParseShortJoinUrl(joinURL, out var joinMeetingIdMeetingInfo))
            {
                //// ChatInfo is deliberately null: a short URL carries no chat thread, and joining
                //// by joinMeetingId takes its coordinates from MeetingInfo alone.
                return (null, joinMeetingIdMeetingInfo);
            }

            var regex = new Regex("https://teams\\.microsoft\\.com.*/(?<thread>[^/]+)/(?<message>[^/]+)\\?context=(?<context>{.*})");
            var match = regex.Match(decodedURL);
            if (!match.Success)
            {
                throw new ArgumentException($"Join URL cannot be parsed: {joinURL}", nameof(joinURL));
            }

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(match.Groups["context"].Value)))
            {
                var ctxt = (Meeting)new DataContractJsonSerializer(typeof(Meeting)).ReadObject(stream);

                if (string.IsNullOrEmpty(ctxt.Tid))
                {
                    throw new ArgumentException("Join URL is invalid: missing Tid", nameof(joinURL));
                }

                var chatInfo = new ChatInfo
                {
                    ThreadId = match.Groups["thread"].Value,
                    MessageId = match.Groups["message"].Value,
                    ReplyChainMessageId = ctxt.MessageId,
                };

                var meetingInfo = new OrganizerMeetingInfo
                {
                    Organizer = new IdentitySet
                    {
                        User = new Identity { Id = ctxt.Oid },
                    },
                };
                meetingInfo.Organizer.User.SetTenantId(ctxt.Tid);

                return (chatInfo, meetingInfo);
            }
        }

        /// <summary>
        /// Tries to parse the short Teams meeting join URL form
        /// <c>https://&lt;host&gt;/meet/&lt;joinMeetingId&gt;?p=&lt;passcode&gt;</c>.
        /// </summary>
        /// <param name="joinURL">The raw, still-encoded join URL.</param>
        /// <param name="meetingInfo">The parsed meeting info, or null when this is not a short join URL.</param>
        /// <returns>True when the URL is a short join URL.</returns>
        private static bool TryParseShortJoinUrl(string joinURL, out JoinMeetingIdMeetingInfo meetingInfo)
        {
            meetingInfo = null;

            if (!Uri.TryCreate(joinURL, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            //// The path must be exactly "/meet/<digits>". Extra or empty path segments (including a
            //// trailing slash or doubled separators) and trailing characters after the digits are
            //// not a short join URL, so they fall through to the long-URL parser and are rejected
            //// instead of being normalized into a plausible-looking meeting id.
            const string ShortJoinPathPrefix = "/meet/";
            var path = uri.AbsolutePath;
            if (!path.StartsWith(ShortJoinPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var joinMeetingId = path.Substring(ShortJoinPathPrefix.Length);
            if (!IsAllDigits(joinMeetingId))
            {
                return false;
            }

            meetingInfo = new JoinMeetingIdMeetingInfo
            {
                JoinMeetingId = joinMeetingId,

                //// Uri.Query excludes any fragment, so "?p=abc#frag" correctly yields "abc".
                Passcode = GetQueryParameter(uri.Query, "p"),
            };

            return true;
        }

        /// <summary>
        /// Determines whether a value is a non-empty run of ASCII digits.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True when the value consists only of ASCII digits.</returns>
        private static bool IsAllDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (var character in value)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a single parameter out of a URL query string.
        /// </summary>
        /// <param name="query">The query component, with or without its leading '?'.</param>
        /// <param name="name">The parameter name to look for.</param>
        /// <returns>The decoded value, or null when the parameter is absent or empty.</returns>
        private static string GetQueryParameter(string query, string name)
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            foreach (var pair in query.TrimStart('?').Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                var separatorIndex = pair.IndexOf('=');
                var key = separatorIndex < 0 ? pair : pair.Substring(0, separatorIndex);

                if (!string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (separatorIndex < 0)
                {
                    return null;
                }

                var value = pair.Substring(separatorIndex + 1);

                //// UnescapeDataString rather than UrlDecode: it does not turn "+" into a space, so
                //// a passcode containing a literal "+" survives intact.
                return value.Length == 0 ? null : Uri.UnescapeDataString(value);
            }

            return null;
        }
    }
}
