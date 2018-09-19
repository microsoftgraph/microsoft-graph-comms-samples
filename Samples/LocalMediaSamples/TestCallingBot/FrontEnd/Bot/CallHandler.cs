// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.TestCallingBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Calls;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Graph.Core.Common;
    using Microsoft.Graph.CoreSDK.Serialization;
    using Microsoft.Graph.StatefulClient;
    using Microsoft.Skype.Bots.Media;
    using Sample.Common.Logging;
    using Sample.TestCallingBot.FrontEnd.Http;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    internal class CallHandler : IDisposable
    {
        /// <summary>
        /// MSI when there is no dominant speaker.
        /// </summary>
        public const uint DominantSpeakerNone = DominantSpeakerChangedEventArgs.None;

        /// <summary>
        /// JSON Serializer for pretty printing.
        /// </summary>
        private static readonly Serializer Serializer = new Serializer(pretty: true);

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
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="statefulCall">Stateful call instance.</param>
        public CallHandler(ICall statefulCall)
        {
            this.Call = statefulCall;

            this.Call.OnUpdated += this.OnCallUpdated;
            if (this.Call.GetLocalMediaSession() != null)
            {
                this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged += this.OnDominantSpeakerChanged;
                this.Call.GetLocalMediaSession().VideoSocket.VideoMediaReceived += this.OnVideoMediaReceived;
            }

            this.Call.Participants.OnUpdated += this.OnParticipantsUpdated;
            var outcome = Serializer.SerializeObject(statefulCall.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Created:\n" + outcome);
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
        /// Gets or sets the application context.
        /// </summary>
        public string AppContext { get; set; }

        /// <summary>
        /// Gets the outcomes log - maintained for easy checking of async server responses.
        /// </summary>
        /// <value>
        /// The outcomes log.
        /// </value>
        public LinkedList<string> OutcomesLogMostRecentFirst { get; } = new LinkedList<string>();

        /// <summary>
        /// Gets the latest screenshot image for this call.
        /// </summary>
        public Bitmap LatestScreenshotImage { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LatestScreenshotImage?.Dispose();

            this.Call.OnUpdated -= this.OnCallUpdated;
            this.Call.GetLocalMediaSession().AudioSocket.DominantSpeakerChanged -= this.OnDominantSpeakerChanged;
            this.Call.GetLocalMediaSession().VideoSocket.VideoMediaReceived -= this.OnVideoMediaReceived;
            this.Call.Participants.OnUpdated -= this.OnParticipantsUpdated;
            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }
        }

        /// <summary>
        /// The set hue.
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

        /// <summary>
        /// Event triggered when the call object is updated.
        /// </summary>
        /// <param name="sender">Call instance.</param>
        /// <param name="args">Event args contains the old values and the new values for the call.</param>
        private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Updated:\n" + outcome);

            // we have a consultative transfer
            if (sender.Resource.State == CallState.Established)
            {
                if (this.AppContext != null && this.AppContext.Equals(Bot.OutboundPlayPromptContext))
                {
                    Task.Run(async () =>
                    {
                        await sender.PlayPromptAsync(new List<MediaPrompt> { Bot.Instance.MediaMap["OutboundPrompt"] }).ConfigureAwait(false);
                        Log.Info(new CallerInfo(), LogContext.FrontEnd, "Started playing OutboundPrompt prompt");
                    });
                }

                var appContext = this.AppContext?.Split('%');
                var item1 = appContext?.FirstOrDefault();
                if ("consultativeTransfer".EqualsIgnoreCase(item1))
                {
                    var invitation = new InvitationParticipantInfo
                    {
                        Identity = Bot.Instance.Client.Calls()[appContext[1]].Resource.Source.Identity,
                        ReplacesCallId = appContext[1],
                    };
                    var transferTask = sender.TransferAsync(invitation);

                    Task.Run(async () =>
                    {
                        await transferTask.ConfigureAwait(false);
                        Log.Info(new CallerInfo(), LogContext.FrontEnd, "Started consultative transfering call");
                    });
                }

                if (Bot.Instance.WelcomePromptQueue.Contains(sender.Id))
                {
                    Task.Run(async () =>
                    {
                        await sender.PlayPromptAsync(new List<MediaPrompt> { Bot.Instance.MediaMap["welcome"] }).ConfigureAwait(false);
                        Log.Info(new CallerInfo(), LogContext.FrontEnd, "Started playing welcome prompt");
                    });
                    if (sender.Resource.Direction == CallDirection.Incoming)
                    {
                        Bot.Instance.WelcomePromptQueue.Remove(sender.Id);
                    }
                }
            }

            // Call state might have changed to established.
            this.Subscribe();
        }

        /// <summary>
        /// Event triggered when participants collection is updated.
        /// </summary>
        /// <param name="sender">Call Participants collection.</param>
        /// <param name="args">Event args containing the added participants and removed participants.</param>
        private void OnParticipantsUpdated(ICallParticipantCollection sender, CollectionEventArgs<ICallParticipant> args)
        {
            if (args.AddedResources != null && args.AddedResources.Any() && Bot.Instance.JoinedMediaType == CallMediaType.Remote)
            {
                Task.Run(async () =>
                {
                    await this.Call.PlayPromptAsync(new List<MediaPrompt> { Bot.Instance.MediaMap["welcome"] }).ConfigureAwait(false);
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, "Started playing welcome prompt");
                });
            }

            foreach (var participant in args.AddedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Added:\n" + outcome);

                participant.OnUpdated += this.OnParticipantUpdated;
            }

            foreach (var participant in args.RemovedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Removed:\n" + outcome);

                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            // Subscribed participant might have left the meeting.
            this.Subscribe();
        }

        /// <summary>
        /// Event triggered when a participant's properties are updated.
        /// </summary>
        /// <param name="sender">Call participant.</param>
        /// <param name="args">Event contains the old values and the new values for the participant.</param>
        private void OnParticipantUpdated(ICallParticipant sender, ResourceEventArgs<Participant> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Participant Updated:\n" + outcome);

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
            CorrelationId.SetCurrentId(this.Call.Id);
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                $"[{this.Call.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]");

            this.subscribedToMsi = e.CurrentDominantSpeaker;
            Task.Run(async () =>
            {
                await this.SubscribeAsync(e.CurrentDominantSpeaker).ConfigureAwait(false);
                Log.Info(new CallerInfo(), LogContext.FrontEnd, "Subscription complete");
            });
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
                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "[{0}]: Capturing image: [VideoMediaReceivedEventArgs(Data=<{1}>, Length={2}, Timestamp={3}, Width={4}, Height={5}, ColorFormat={6}, FrameRate={7})]",
                    this.Call.Id,
                    e.Buffer.Data.ToString(),
                    e.Buffer.Length,
                    e.Buffer.Timestamp,
                    e.Buffer.VideoFormat.Width,
                    e.Buffer.VideoFormat.Height,
                    e.Buffer.VideoFormat.VideoColorFormat,
                    e.Buffer.VideoFormat.FrameRate);

                byte[] buffer = new byte[e.Buffer.Length];
                Marshal.Copy(e.Buffer.Data, buffer, 0, (int)e.Buffer.Length);

                if (DateTime.Now > this.lastVideoCapturedTimeUtc + this.videoCaptureFrequency)
                {
                    // Step 1: Update screenshot

                    // Update the last capture timestamp
                    this.lastVideoCapturedTimeUtc = DateTime.Now;

                    // Transform to bitmap object
                    Bitmap bmpObject = MediaUtils.TransformNv12ToBmpFaster(
                        buffer,
                        e.Buffer.VideoFormat.Width,
                        e.Buffer.VideoFormat.Height);

                    // Update the bitmap cache
                    this.LatestScreenshotImage = bmpObject;
                }

                // 33 ms frequency ~ 30 fps
                if (DateTime.Now > this.lastVideoSentTimeUtc + TimeSpan.FromMilliseconds(33))
                {
                    this.lastVideoSentTimeUtc = DateTime.Now;

                    // Step 2: Send Video with added hue
                    this.AddHue(this.hueColor, buffer, e.Buffer.VideoFormat.Width, e.Buffer.VideoFormat.Height);

                    // Use the real length of the data (Media may send us a larger buffer)
                    VideoFormat sendVideoFormat = this.GetSendVideoFormat(e.Buffer.VideoFormat);
                    var videoSendBuffer = new VideoSendBuffer(buffer, (uint)(e.Buffer.VideoFormat.Width * e.Buffer.VideoFormat.Height * 12 / 8), sendVideoFormat);
                    this.Call.GetLocalMediaSession().VideoSocket.Send(videoSendBuffer);
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    new CallerInfo(),
                    LogContext.Media,
                    $"{this.Call.Id} Exception in VideoMediaReceived {ex}");
            }

            e.Buffer.Dispose();
        }

        /// <summary>
        /// add hue method.
        /// </summary>
        /// <param name="hueColor">
        /// The hue color.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        private void AddHue(HueColor hueColor, byte[] buffer, int width, int height)
        {
            int start = 0;
            int widthXheight = width * height;
            int count = widthXheight / 2, length = buffer.Length;

            while (start < length)
            {
                // skip y
                start += widthXheight;

                // read u,v
                int max = Math.Min(start + count + 1, length);

                for (int i = start; i < max; i += 2)
                {
                    switch (hueColor)
                    {
                        case HueColor.Red:
                            this.SubtractWithoutRollover(buffer, i, 16);
                            this.AddWithoutRollover(buffer, i + 1, 50);
                            break;

                        case HueColor.Blue:
                            this.AddWithoutRollover(buffer, i, 50);
                            this.SubtractWithoutRollover(buffer, i + 1, 8);
                            break;

                        case HueColor.Green:
                            this.SubtractWithoutRollover(buffer, i, 33);
                            this.SubtractWithoutRollover(buffer, i + 1, 41);
                            break;

                        default: break;
                    }
                }

                start += count;
            }
        }

        /// <summary>
        /// subtract without rollover.
        /// </summary>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private void SubtractWithoutRollover(byte[] buffer, int index, byte value)
        {
            if (buffer[index] >= value)
            {
                buffer[index] -= value;
            }
            else
            {
                buffer[index] = byte.MinValue;
            }

            return;
        }

        /// <summary>
        /// Add without rollover.
        /// </summary>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private void AddWithoutRollover(byte[] buffer, int index, byte value)
        {
            int val = Convert.ToInt32(buffer[index]) + value;
            buffer[index] = (byte)Math.Min(val, byte.MaxValue);
        }

        /// <summary>
        /// The get sending video format.
        /// </summary>
        /// <param name="videoFormat">
        /// The video format.
        /// </param>
        /// <returns>
        /// The <see cref="VideoFormat"/>.
        /// </returns>
        private VideoFormat GetSendVideoFormat(VideoFormat videoFormat)
        {
            VideoFormat sendVideoFormat;
            switch (videoFormat.Width)
            {
                case 270:
                    sendVideoFormat = VideoFormat.NV12_270x480_15Fps;
                    break;

                case 320:
                    sendVideoFormat = VideoFormat.NV12_320x180_15Fps;
                    break;

                case 360:
                    sendVideoFormat = VideoFormat.NV12_360x640_15Fps;
                    break;

                case 424:
                    sendVideoFormat = VideoFormat.NV12_424x240_15Fps;
                    break;

                case 480:
                    if (videoFormat.Height == 270)
                    {
                        sendVideoFormat = VideoFormat.NV12_480x270_15Fps;
                        break;
                    }

                    sendVideoFormat = VideoFormat.NV12_480x848_30Fps;
                    break;

                case 640:
                    sendVideoFormat = VideoFormat.NV12_640x360_15Fps;
                    break;

                case 720:
                    sendVideoFormat = VideoFormat.NV12_720x1280_30Fps;
                    break;

                case 848:
                    sendVideoFormat = VideoFormat.NV12_848x480_30Fps;
                    break;

                case 960:
                    sendVideoFormat = VideoFormat.NV12_960x540_30Fps;
                    break;

                case 1280:
                    sendVideoFormat = VideoFormat.NV12_1280x720_30Fps;
                    break;

                case 1920:
                    sendVideoFormat = VideoFormat.NV12_1920x1080_30Fps;
                    break;

                default:
                    sendVideoFormat = VideoFormat.NV12_424x240_15Fps;
                    break;
            }

            return sendVideoFormat;
        }

        /// <summary>
        /// Subscribe to the video stream of a participant. This function is called when dominant speaker notification is received and when roster changes
        /// When invoked on dominant speaker change, look if the participant is sharing their video. If yes then subscribe else choose the first participant in the list sharing their video
        /// When invoked on roster change, verify if the previously subscribed-to participant is still in the roster and sending video.
        /// </summary>
        /// <param name="msi">
        /// MSI of dominant speaker or previously subscribed to MSI depending on where it is invoked.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task SubscribeAsync(uint msi)
        {
            // Only subscribe when call is in established state.
            if (this.Call.Resource.State != CallState.Established)
            {
                return;
            }

            try
            {
                Log.Info(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    $"[Call ID: {this.Call.Id}] Received subscribe request for Msi {msi}");

                ICallParticipant participant = this.GetParticipantForParticipantsChange(msi);
                if (participant == null)
                {
                    this.subscribedToParticipant = null;

                    Log.Info(
                        new CallerInfo(),
                        LogContext.FrontEnd,
                        $"[{this.Call.Id}] Could not find valid participant using MSI {msi}");

                    return;
                }

                // if we have already subscribed earlier, skip the subscription
                if (this.subscribedToParticipant?.Id.Equals(participant.Id, StringComparison.OrdinalIgnoreCase) == true)
                {
                    Log.Info(
                        new CallerInfo(),
                        LogContext.FrontEnd,
                        $"[{this.Call.Id}] Already subscribed to {participant.Id}. So skipping subscription");
                }
                else
                {
                    var outcome = Serializer.SerializeObject(participant.Resource);
                    this.OutcomesLogMostRecentFirst.AddFirst("Dominant Speaker Changed:\n" + outcome);

                    Log.Info(
                        new CallerInfo(),
                        LogContext.FrontEnd,
                        $"[{this.Call.Id}] Subscribing to {participant.Id} using MSI {msi}");
                }

                await participant.SubscribeVideoAsync(videoResolution: VideoResolutionFormat.Hd1080p).ConfigureAwait(false);

                // Set the dominant speaker after subscribe completed successfully.
                // If subscribe fails, another subscribe can set it properly.
                this.subscribedToParticipant = participant.Resource;
            }
            catch (Exception ex)
            {
                var outcome = Serializer.SerializeObject(ex);
                this.OutcomesLogMostRecentFirst.AddFirst("Dominant Speaker Failed:\n" + outcome);

                Log.Error(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    $"[{this.Call.Id}] Subscribe threw exception {ex}");
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
            if (Bot.Instance.JoinedMediaType == CallMediaType.Local)
            {
                uint prevSubscribedMsi = this.subscribedToMsi;
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Call.Id}] Subscribing to: {prevSubscribedMsi}");

                Task.Run(async () =>
                {
                    await this.SubscribeAsync(prevSubscribedMsi).ConfigureAwait(false);
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, "Subscription complete");
                });
            }
        }

        /// <summary>
        /// Gets the participant for roster change.
        /// </summary>
        /// <param name="dominantSpeakerMsi">
        /// The dominant Speaker MSI.
        /// </param>
        /// <returns>
        /// The <see cref="ICallParticipant"/>.
        /// </returns>
        private ICallParticipant GetParticipantForParticipantsChange(uint dominantSpeakerMsi)
        {
            if (this.Call.Participants.Count < 1)
            {
                Log.Warning(
                    new CallerInfo(),
                    LogContext.FrontEnd,
                    $"[{this.Call.Id}] Did not receive rosterupdate notification yet");
                return null;
            }

            ICallParticipant firstParticipant = null;
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
