// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Bot
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    public class CallHandler : HeartbeatHandler
    {
        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        /// <summary>
        /// How long the timer should wait before ending the call.
        /// </summary>
        private const double WaitForMs = 1000 * 60 * 5;

        /// <summary>
        /// The time between each video frame capturing.
        /// </summary>
        private readonly TimeSpan videoCaptureFrequency = TimeSpan.FromMilliseconds(2000);

        /// <summary>
        /// The time stamp when video image was updated last.
        /// </summary>
        private DateTime lastVideoCapturedTimeUtc = DateTime.MinValue;

        /// <summary>
        /// The time stamp when video was sent last.
        /// </summary>
        private DateTime lastVideoSentTimeUtc = DateTime.MinValue;

        /// <summary>
        /// The MediaStreamId of the last dominant speaker.
        /// </summary>
        private uint subscribedToMsi = DominantSpeakerNone;

        /// <summary>
        /// The MediaStreamId of the participant to which the video channel is currently subscribed to.
        /// </summary>
        private Participant subscribedToParticipant;

        /// <summary>
        /// Hue Color for the video looped back.
        /// </summary>
        private HueColor hueColor = HueColor.Blue;

        /// <summary>
        /// Count of incoming messages to log.
        /// </summary>
        private int maxIngestFrameCount = 100;

        /// <summary>
        /// The Timer to end the call.
        /// </summary>
        private Timer endCallTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">Stateful call instance.</param>
        public CallHandler(ICall statefulCall)
            : base(TimeSpan.FromMinutes(10), statefulCall?.GraphLogger)
        {
            this.Call = statefulCall;
            this.Call.OnUpdated += this.OnCallUpdated;
            if (this.Call.GetLocalMediaSession() != null)
            {
                this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;
                this.Call.GetLocalMediaSession().VideoSocket.VideoMediaReceived += this.OnVideoMediaReceived;
            }

            this.Call.Participants.OnUpdated += this.OnParticipantsUpdated;
            this.endCallTimer = new Timer(CallHandler.WaitForMs);
            this.endCallTimer.Enabled = false;
            this.endCallTimer.AutoReset = false;
            this.endCallTimer.Elapsed += this.OnTimerElapsed;
        }

        /// <summary>
        /// The hue color.
        /// </summary>
        internal enum HueColor
        {
            /// <summary>
            /// The red.
            /// </summary>
            Red,

            /// <summary>
            /// The blue.
            /// </summary>
            Blue,

            /// <summary>
            /// The green.
            /// </summary>
            Green,
        }

        /// <summary>
        /// Gets the call object.
        /// </summary>
        public ICall Call { get; }

        /// <summary>
        /// Gets the latest screenshot image for this call.
        /// </summary>
        public Bitmap LatestScreenshotImage { get; private set; }

        /// <summary>
        /// Get hue.
        /// </summary>
        /// <returns>Current hue.</returns>
        internal string GetHue()
        {
            return this.hueColor.ToString();
        }

        /// <summary>
        /// Set hue.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        internal void SetHue(string color)
        {
            if (Enum.TryParse(color, true, out HueColor tempHueColor))
            {
                this.hueColor = tempHueColor;
            }
            else
            {
                throw new ArgumentException($"invalid color ({color})", nameof(color));
            }
        }

        /// <inheritdoc/>
        protected override Task HeartbeatAsync(ElapsedEventArgs args)
        {
            return this.Call.KeepAliveAsync();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.LatestScreenshotImage?.Dispose();

            this.Call.OnUpdated -= this.OnCallUpdated;
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged -= this.OnDominantSpeakerChanged;
            this.Call.GetLocalMediaSession().VideoSocket.VideoMediaReceived -= this.OnVideoMediaReceived;
            this.Call.Participants.OnUpdated -= this.OnParticipantsUpdated;
            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.endCallTimer.Elapsed -= this.OnTimerElapsed;
        }

        /// <summary>
        /// Event triggered when the call object is updated.
        /// </summary>
        /// <param name="sender">Call instance.</param>
        /// <param name="args">Event args contains the old values and the new values for the call.</param>
        private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            // Call state might have changed to established.
            this.Subscribe();
        }

        /// <summary>
        /// Event triggered when participants collection is updated.
        /// </summary>
        /// <param name="sender">Call Participants collection.</param>
        /// <param name="args">Event args containing the added participants and removed participants.</param>
        private void OnParticipantsUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                participant.OnUpdated += this.OnParticipantUpdated;
            }

            foreach (var participant in args.RemovedResources)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            bool nonBotParticipants = false;
            foreach (var participant in sender)
            {
                var isBot = participant.Resource.Info.Identity.Application != null;
                if (!isBot)
                {
                    nonBotParticipants = true;
                    break;
                }
            }

            if (nonBotParticipants)
            {
                this.endCallTimer.Stop();
            }
            else
            {
                this.endCallTimer.Start();
            }

            // Subscribed participant might have left the meeting.
            this.Subscribe();
        }

        /// <summary>
        /// When the end call timer elapses, end the call.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="args">Event args containing the elapsed event information.</param>
        private void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            _ = this.Call.DeleteAsync().ForgetAndLogExceptionAsync(this.Call.GraphLogger);
        }

        /// <summary>
        /// Event triggered when a participant's properties are updated.
        /// </summary>
        /// <param name="sender">Call participant.</param>
        /// <param name="args">Event contains the old values and the new values for the participant.</param>
        private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            // Subscribed participant might have disconnected video.
            this.Subscribe();
        }

        /// <summary>
        /// Listen for dominant speaker changes in the conference.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The dominant speaker changed event arguments.
        /// </param>
        private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
        {
            this.GraphLogger.Info($"[{this.Call.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]");

            this.subscribedToMsi = e.CurrentDominantSpeaker;

            this.Subscribe(e.CurrentDominantSpeaker);
        }

        /// <summary>
        /// Save screenshots when we receive video from subscribed participant.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The video media received arguments.
        /// </param>
        private void OnVideoMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            try
            {
                if (Interlocked.Decrement(ref this.maxIngestFrameCount) > 0)
                {
                    this.GraphLogger.Info(
                        $"[{this.Call.Id}]: Capturing image: [VideoMediaReceivedEventArgs(Data=<{e.Buffer.Data.ToString()}>, " +
                        $"Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp}, Width={e.Buffer.VideoFormat.Width}, " +
                        $"Height={e.Buffer.VideoFormat.Height}, ColorFormat={e.Buffer.VideoFormat.VideoColorFormat}, FrameRate={e.Buffer.VideoFormat.FrameRate})]");
                }

                // 33 ms frequency ~ 30 fps
                if (DateTime.Now > this.lastVideoSentTimeUtc + TimeSpan.FromMilliseconds(33))
                {
                    this.lastVideoSentTimeUtc = DateTime.Now;

                    // Step 1: Send Video with added hue
                    byte[] buffer = e.Buffer.ApplyHue(this.hueColor);

                    // Use the real length of the data (Media may send us a larger buffer)
                    VideoFormat sendVideoFormat = e.Buffer.VideoFormat.GetSendVideoFormat();
                    var videoSendBuffer = new VideoSendBuffer(buffer, (uint)buffer.Length, sendVideoFormat);
                    this.Call.GetLocalMediaSession().VideoSocket.Send(videoSendBuffer);

                    if (DateTime.Now > this.lastVideoCapturedTimeUtc + this.videoCaptureFrequency)
                    {
                        // Step 2: Update screenshot of image with hue applied.

                        // Update the last capture timestamp
                        this.lastVideoCapturedTimeUtc = DateTime.Now;

                        // Transform to bitmap object
                        Bitmap bmpObject = MediaUtils.TransformNv12ToBmpFaster(
                            buffer,
                            e.Buffer.VideoFormat.Width,
                            e.Buffer.VideoFormat.Height,
                            this.GraphLogger);

                        // Update the bitmap cache
                        this.LatestScreenshotImage = bmpObject;
                    }
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"[{this.Call.Id}] Exception in VideoMediaReceived");
            }

            e.Buffer.Dispose();
        }

        /// <summary>
        /// Subscribe to the video stream of a participant. This function is called when dominant speaker notification is received and when roster changes
        /// When invoked on dominant speaker change, look if the participant is sharing their video. If yes then subscribe else choose the first participant in the list sharing their video
        /// When invoked on roster change, verify if the previously subscribed-to participant is still in the roster and sending video.
        /// </summary>
        /// <param name="msi">
        /// MSI of dominant speaker or previously subscribed to MSI depending on where it is invoked.
        /// </param>
        private void Subscribe(uint msi)
        {
            // Only subscribe when call is in established state.
            if (this.Call.Resource.State != CallState.Established)
            {
                return;
            }

            try
            {
                this.GraphLogger.Info($"[{this.Call.Id}] Received subscribe request for Msi {msi}");

                IParticipant participant = this.GetParticipantForParticipantsChange(msi);
                if (participant == null)
                {
                    this.subscribedToParticipant = null;

                    this.GraphLogger.Info($"[{this.Call.Id}] Could not find valid participant using MSI {msi}");

                    return;
                }

                // if we have already subscribed earlier, skip the subscription
                if (this.subscribedToParticipant?.Id.Equals(participant.Id, StringComparison.OrdinalIgnoreCase) == true)
                {
                    this.GraphLogger.Info($"[{this.Call.Id}] Already subscribed to {participant.Id}. So skipping subscription");
                }
                else
                {
                    this.GraphLogger.Info($"[{this.Call.Id}] Subscribing to {participant.Id} using MSI {msi}");
                }

                if (uint.TryParse(participant.Resource.MediaStreams.FirstOrDefault(m => m.MediaType == Modality.Video)?.SourceId, out msi))
                {
                    this.Call.GetLocalMediaSession().VideoSocket.Subscribe(VideoResolution.HD1080p, msi);
                }

                // Set the dominant speaker after subscribe completed successfully.
                // If subscribe fails, another subscribe can set it properly.
                this.subscribedToParticipant = participant.Resource;
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"[{this.Call.Id}] Subscribe threw exception");
            }
        }

        /// <summary>
        /// Subscribe to the video stream of a participant. This function is called when call is updated or roster changes
        /// When invoked look if the participant is sharing their video.
        /// If yes then subscribe else choose the first participant in the list sharing their video
        /// When invoked on call update, verify that we are in an established state and subscribe to a participant video.
        /// When invoked on roster change, verify if the previously subscribed-to participant is still in the roster and sending video.
        /// </summary>
        private void Subscribe()
        {
            uint prevSubscribedMsi = this.subscribedToMsi;
            this.GraphLogger.Info($"[{this.Call.Id}] Subscribing to: {prevSubscribedMsi}");

            this.Subscribe(prevSubscribedMsi);
        }

        /// <summary>
        /// Gets the participant for roster change.
        /// </summary>
        /// <param name="dominantSpeakerMsi">
        /// The dominant Speaker MSI.
        /// </param>
        /// <returns>
        /// The <see cref="IParticipant"/>.
        /// </returns>
        private IParticipant GetParticipantForParticipantsChange(uint dominantSpeakerMsi)
        {
            if (this.Call.Participants.Count < 1)
            {
                this.GraphLogger.Warn($"[{this.Call.Id}] Did not receive rosterupdate notification yet");
                return null;
            }

            IParticipant firstParticipant = null;
            foreach (var participant in this.Call.Participants)
            {
                var audioStream = participant.Resource.MediaStreams.FirstOrDefault(stream => stream.MediaType == Modality.Audio);
                if (audioStream == null)
                {
                    continue;
                }

                var videoStream = participant.Resource.MediaStreams.FirstOrDefault(stream =>
                {
                    var isVideo = stream.MediaType == Modality.Video;
                    var isSending = stream.Direction == MediaDirection.SendOnly || stream.Direction == MediaDirection.SendReceive;
                    return isVideo && isSending;
                });
                if (videoStream == null)
                {
                    continue;
                }

                // We found the dominant speaker and they have an outbound video stream.
                if (dominantSpeakerMsi.ToString().Equals(audioStream.SourceId, StringComparison.OrdinalIgnoreCase))
                {
                    return participant;
                }

                // cache the first participant.. just in case dominant speaker is not sending video
                if (firstParticipant == null ||
                    this.subscribedToParticipant?.Id.Equals(participant.Id, StringComparison.OrdinalIgnoreCase) == true)
                {
                    firstParticipant = participant;
                }
            }

            // If dominant speaker is not sending video or if dominant speaker has exited the conference, choose the first participant sending video
            return firstParticipant;
        }
    }
}
