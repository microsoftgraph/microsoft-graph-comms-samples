// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Client;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Skype.Bots.Media;
    using Sample.AudioVideoPlaybackBot.FrontEnd;
    using Sample.AudioVideoPlaybackBot.FrontEnd.Http;
    using Sample.AudioVideoPlaybackBot.FrontEnd.UrlUtilities;
    using Sample.Common;
    using Sample.Common.Authentication;
    using Sample.Common.Logging;
    using Sample.Common.Meetings;
    using Sample.Common.OnlineMeetings;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    internal class Bot : IDisposable
    {
        /// <summary>
        /// Initializes static members of the <see cref="Bot"/> class.
        /// </summary>
        static Bot()
        {
            // Create a flag to prevent recursive calls
            bool isResolvingAssembly = false;

            // Register assembly resolver
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // Prevent recursive calls that can cause StackOverflowException
                if (isResolvingAssembly)
                {
                    return null;
                }

                try
                {
                    isResolvingAssembly = true;

                    var requestedAssembly = new System.Reflection.AssemblyName(args.Name);

                    if (requestedAssembly.Name == "System.Text.Json")
                    {
                        EventLog.WriteEntry("AudioVideoPlaybackService", $"Resolving System.Text.Json: {args.Name}", EventLogEntryType.Warning);

                        // Try to load from the output directory
                        var outputDir = AppDomain.CurrentDomain.BaseDirectory;
                        var jsonDllPath = Path.Combine(outputDir, "System.Text.Json.dll");

                        if (System.IO.File.Exists(jsonDllPath))
                        {
                            try
                            {
                                // Use LoadFrom instead of LoadFile to utilize the assembly binding context
                                return System.Reflection.Assembly.LoadFrom(jsonDllPath);
                            }
                            catch (Exception ex)
                            {
                                EventLog.WriteEntry("AudioVideoPlaybackService", $"Error loading System.Text.Json from file: {ex.Message}", EventLogEntryType.Error);
                            }
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Exception in assembly resolver: {ex.Message}", EventLogEntryType.Error);
                    return null;
                }
                finally
                {
                    isResolvingAssembly = false;
                }
            };
        }

        /// <summary>
        /// Gets the instance of the bot.
        /// </summary>
        public static Bot Instance { get; } = new Bot();

        /// <summary>
        /// Gets the Graph Logger instance.
        /// </summary>
        public IGraphLogger Logger { get; private set; }

        /// <summary>
        /// Gets the sample log observer.
        /// </summary>
        public SampleObserver Observer { get; private set; }

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        public ICommunicationsClient Client { get; private set; }

        /// <summary>
        /// Gets the online meeting.
        /// </summary>
        /// <value>
        /// The online meeting.
        /// </value>
        public OnlineMeetingHelper OnlineMeetings { get; private set; }

        /// <summary>
        /// Gets the configuration instance.
        /// </summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall"/> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallController.JoinCallBody joinCallBody)
        {
            try
            {
                // Check if the client is initialized
                if (this.Client == null)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", "Client is null in JoinCallAsync. Bot may not be properly initialized.", EventLogEntryType.Error);

                    // Try to re-initialize if possible
                    if (Service.Instance != null)
                    {
                        EventLog.WriteEntry("AudioVideoPlaybackService", "Attempting to re-initialize the bot", EventLogEntryType.Warning);
                        try
                        {
                            this.Initialize(Service.Instance, this.Logger ?? new SimpleGraphLogger("AudioVideoPlaybackBot"));

                            // Check if initialization was successful
                            if (this.Client == null)
                            {
                                throw new InvalidOperationException("Bot initialization failed. The communications client is still null.");
                            }

                            EventLog.WriteEntry("AudioVideoPlaybackService", "Bot re-initialization successful", EventLogEntryType.Information);
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry("AudioVideoPlaybackService", $"Bot re-initialization failed: {ex.Message}", EventLogEntryType.Error);
                            throw new InvalidOperationException("Bot is not properly initialized and re-initialization failed.", ex);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Bot is not properly initialized. The communications client is null and Service.Instance is not available.");
                    }
                }

                // Normalize the join URL before parsing it
                joinCallBody.JoinURL = Sample.AudioVideoPlaybackBot.FrontEnd.UrlUtilities.UrlNormalizer.NormalizeTeamsMeetingUrl(joinCallBody.JoinURL);

                // Log the normalized URL - Add null check before using logger
                if (this.Logger != null)
                {
                    this.Logger.Info($"Normalized join URL: {joinCallBody.JoinURL}");
                    this.Logger.Info("Join call requested");
                }
                else
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", "Logger is null in JoinCallAsync", EventLogEntryType.Error);

                    // Initialize logger if possible
                    this.Logger = new SimpleGraphLogger("AudioVideoPlaybackBot");
                }

                EventLog.WriteEntry("AudioVideoPlaybackService", "Bot.cs JoinCallAsync called", EventLogEntryType.Warning);

                // A tracking id for logging purposes. Helps identify this call in logs.
                var scenarioId = Guid.NewGuid();

                // Ensure Newtonsoft.Json is properly loaded before proceeding
                try
                {
                    // Force load Newtonsoft.Json assembly to ensure it's available
                    var assembly = System.Reflection.Assembly.Load("Newtonsoft.Json");
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Successfully loaded Newtonsoft.Json assembly: {assembly.FullName}", EventLogEntryType.Information);

                    // Verify the serialization functionality works
                    var testObject = new { Test = "Test" };
                    var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(testObject);
                    EventLog.WriteEntry("AudioVideoPlaybackService", "JSON serialization test successful", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Error loading or testing Newtonsoft.Json: {ex.Message}", EventLogEntryType.Error);
                    throw new InvalidOperationException("Failed to initialize JSON serialization components required for call joining.", ex);
                }

                MeetingInfo meetingInfo;
                ChatInfo chatInfo;
                if (!string.IsNullOrWhiteSpace(joinCallBody.VideoTeleconferenceId))
                {
                    // Video Tele-Conference id is a cloud-video-interop numeric meeting id.
                    var onlineMeeting = await this.OnlineMeetings
                        .GetOnlineMeetingAsync(joinCallBody.TenantId, joinCallBody.VideoTeleconferenceId, scenarioId)
                        .ConfigureAwait(false);

                    meetingInfo = new OrganizerMeetingInfo { Organizer = onlineMeeting.Participants.Organizer.Identity, };
                    chatInfo = onlineMeeting.ChatInfo;
                }
                else
                {
                    (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);
                }

                var tenantId =
                    joinCallBody.TenantId ??
                    (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId();
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

                ICall statefulCall = null;
                CallHandler callHandler = null;
                try
                {
                    // Create the BotMediaStream before the call is added to make sure that all media events are subscribed to.
                    // Before adding a new call, which is equivalent to negotiating it. We need to make sure that all media events are subscribed to.
                    // It is possible that media will start to flow, while the call is being processed.
                    var botMediaStream = new BotMediaStream(mediaSession, this.Logger.CreateShim("BotMediaStream", scenarioId));

                    EventLog.WriteEntry("AudioVideoPlaybackService", "About to add call via Client.Calls().AddAsync", EventLogEntryType.Information);
                    statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Call added successfully with ID: {statefulCall?.Id}", EventLogEntryType.Information);

                    callHandler = new CallHandler(statefulCall, botMediaStream);
                    this.CallHandlers.TryAdd(statefulCall.Id, callHandler);
                    statefulCall.GraphLogger.Info($"Call creation complete: {statefulCall.Id}");
                }
                catch (Exception ex)
                {
                    // clean up
                    callHandler?.Dispose();
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Error joining call: {ex.ToString()}", EventLogEntryType.Error);
                    throw;
                }

                return statefulCall;
            }
            catch (EntryPointNotFoundException ex)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"EntryPointNotFoundException in JoinCallAsync: {ex.ToString()}", EventLogEntryType.Error);
                throw new InvalidOperationException("Failed to join call due to missing dependency. The Newtonsoft.Json library may not be properly loaded.", ex);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Unexpected error in JoinCallAsync: {ex.ToString()}", EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Changes bot's screen sharing role async.
        /// </summary>
        /// <param name="callLegId">which call to change role on.</param>
        /// <param name="role">The role to change to.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ChangeSharingRoleAsync(string callLegId, ScreenSharingRole role)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            var call = this.Client.Calls()[callLegId];
            if (call == null)
            {
                throw new ArgumentNullException($"No calls found for {callLegId}");
            }

            await call.ChangeScreenSharingRoleAsync(role)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Observer?.Dispose();
            this.Observer = null;
            this.Logger = null;
            this.Client?.Dispose();
            this.Client = null;
            this.OnlineMeetings = null;
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        /// <param name="service">Service instance.</param>
        /// <param name="logger">Graph logger.</param>
        internal void Initialize(Service service, IGraphLogger logger)
        {
            // Check if service is null
            if (service == null)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", "Service parameter is null in Initialize", EventLogEntryType.Error);
                throw new ArgumentNullException(nameof(service));
            }

            // Store the configuration
            this.Configuration = service.Configuration;

            // Check if logger is null and create a default one if needed
            if (logger == null)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", "Provided logger is null, creating a default one", EventLogEntryType.Warning);
                logger = new SimpleGraphLogger("AudioVideoPlaybackBot");
            }

            // Only check if already initialized, don't throw an exception
            if (this.Logger != null && this.Client != null)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", "Bot already initialized, skipping initialization", EventLogEntryType.Warning);
                return;
            }

            this.Logger = logger;
            this.Observer = new SampleObserver(logger);
            EventLog.WriteEntry("AudioVideoPlaybackService", "Starting Bot initialization", EventLogEntryType.Warning);

            // Explicitly load System.Text.Json and copy to GAC if needed
            try
            {
                var outputDir = AppDomain.CurrentDomain.BaseDirectory;
                var jsonDllPath = Path.Combine(outputDir, "System.Text.Json.dll");

                if (System.IO.File.Exists(jsonDllPath))
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Found System.Text.Json at: {jsonDllPath}", EventLogEntryType.Warning);

                    // Don't try to pre-load here - let the resolver handle it when needed
                    EventLog.WriteEntry("AudioVideoPlaybackService", "System.Text.Json found - will be loaded by resolver when needed", EventLogEntryType.Warning);
                }
                else
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"System.Text.Json.dll not found at: {jsonDllPath}", EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Error checking for System.Text.Json: {ex.Message}", EventLogEntryType.Error);
            }

            // Continue with initialization...
            var name = this.GetType().Assembly.GetName().Name;

            // Add this near the beginning of the Initialize method
            if (service.Configuration != null)
            {
                // Log the tenant ID and app ID being used
                var tenantId = service.Configuration.GetType().GetProperty("TenantId")?.GetValue(service.Configuration)?.ToString();
                var appId = service.Configuration.GetType().GetProperty("AadAppId")?.GetValue(service.Configuration)?.ToString();

                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Initializing with TenantId: {tenantId}, AppId: {appId}",
                    EventLogEntryType.Warning);
            }

            // Wrap the builder creation in a try-catch to get more detailed error information
            try
            {
                // Setup certificate validation before creating the client
                // This is for development environments only - remove for production
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    "Setting up certificate validation callback for development environment",
                    EventLogEntryType.Information);

                // Add a certificate validation callback that accepts all certificates
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;

                var builder = new CommunicationsClientBuilder(
                    name,
                    service.Configuration.AadAppId,
                    this.Logger);

                // var authProvider = this.CreateAuthenticationProvider();
                var authProvider = new AuthenticationProvider(
                    name,
                    service.Configuration.AadAppId,
                    service.Configuration.AadAppSecret,
                    this.Logger);

                builder.SetAuthenticationProvider(authProvider);
                builder.SetNotificationUrl(service.Configuration.CallControlBaseUrl);

                // Add certificate validation settings to media platform settings
                var mediaSettings = service.Configuration.MediaPlatformSettings;

                // Media platform certificate validation settings
                EventLog.WriteEntry("AudioVideoPlaybackService", "Configuring media platform certificate settings", EventLogEntryType.Warning);

                // Start with the original settings
                var settings = service.Configuration.MediaPlatformSettings;

                // Override just the properties we need to change
                settings.MediaPlatformInstanceSettings.ServiceFqdn = "8.tcp.ngrok.io";
                settings.MediaPlatformInstanceSettings.InstanceInternalPort = 8445;
                settings.MediaPlatformInstanceSettings.InstancePublicPort = 12561;
                settings.MediaPlatformInstanceSettings.CertificateThumbprint = "CF5B7AD6F60C1F47580F3703469D5EB7E38BDF5A";

                // Log the settings to verify
                EventLog.WriteEntry(
                    "AudioVideoPlaybackService",
                    $"Configuring media settings with AppId: {settings.ApplicationId}, FQDN: {settings.MediaPlatformInstanceSettings.ServiceFqdn}",
                    EventLogEntryType.Information);

                builder.SetMediaPlatformSettings(settings);

                // builder.SetMediaPlatformSettings(service.Configuration.MediaPlatformSettings);
                builder.SetServiceBaseUrl(service.Configuration.PlaceCallEndpointUrl);

                this.Client = builder.Build();
                this.Client.Calls().OnIncoming += this.CallsOnIncoming;
                this.Client.Calls().OnUpdated += this.CallsOnUpdated;

                this.OnlineMeetings = new OnlineMeetingHelper(authProvider, service.Configuration.PlaceCallEndpointUrl);
                EventLog.WriteEntry("AudioVideoPlaybackService", "Initialize complete Bot.cs", EventLogEntryType.Warning);

                // Verify that the client was successfully created
                if (this.Client == null)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", "Failed to initialize the communications client", EventLogEntryType.Error);
                    throw new InvalidOperationException("Failed to initialize the communications client");
                }

                EventLog.WriteEntry("AudioVideoPlaybackService", "Bot initialization completed successfully", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", $"Failed to initialize bot: {ex.ToString()}", EventLogEntryType.Error);
                throw; // Re-throw to maintain original behavior
            }
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task<bool> EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall call);
            }

            return false;
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">
        /// The media session identifier.
        /// This should be a unique value for each call.
        /// </param>
        /// <returns>The <see cref="ILocalMediaSession"/>.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default(Guid))
        {
            // Check if client is initialized
            if (this.Client == null)
            {
                EventLog.WriteEntry("AudioVideoPlaybackService", "Client is null in CreateLocalMediaSession", EventLogEntryType.Error);
                throw new InvalidOperationException("Communications client is not initialized");
            }

            var videoSocketSettings = new List<VideoSocketSettings>
            {
                // add the main video socket sendrecv capable
                new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    ReceiveColorFormat = VideoColorFormat.H264,

                    // We loop back the video in this sample. The MediaPlatform always sends only NV12 frames.
                    // So include only NV12 video in supportedSendVideoFormats
                    SupportedSendVideoFormats = SampleConstants.SupportedSendVideoFormats,

                    MaxConcurrentSendStreams = 1,
                },
            };

            // create the receive only sockets settings for the multiview support
            for (int i = 0; i < SampleConstants.NumberOfMultiviewSockets; i++)
            {
                videoSocketSettings.Add(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.H264,
                });
            }

            // Create the VBSS socket settings
            var vbssSocketSettings = new VideoSocketSettings
            {
                StreamDirections = StreamDirection.Recvonly,
                ReceiveColorFormat = VideoColorFormat.H264,
                MediaType = MediaType.Vbss,
                SupportedSendVideoFormats = new List<VideoFormat>
                {
                    // fps 1.875 is required for h264 in vbss scenario.
                    VideoFormat.H264_1920x1080_1_875Fps,
                },
            };

            // create media session object, this is needed to establish call connectionS
            var mediaSession = this.Client.CreateMediaSession(
                new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    SupportedAudioFormat = AudioFormat.Pcm16K,
                },
                videoSocketSettings,
                vbssSocketSettings,
                mediaSessionId: mediaSessionId);
            return mediaSession;
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TEntity}"/> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call and start video playback
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection"/> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}"/> instance containing the event data.</param>
        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                call.GraphLogger.Info($"Call with id: {call.Id} and {call.ScenarioId} was added");
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
        /// <param name="callLegId">
        /// The call leg id.
        /// </param>
        /// <returns>
        /// The <see cref="CallHandler"/>.
        /// </returns>
        /// <exception cref="ObjectNotFoundException">
        /// Throws an exception if handler is not found.
        /// </exception>
        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!this.CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ObjectNotFoundException($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}