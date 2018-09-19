// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BotMediaStream.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The bot media stream.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph.Calls.Media;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.Skype.Internal.Media.Services.Common;
    using Sample.Common.Logging;

    /// <summary>
    /// Class responsible for streaming audio and video.
    /// </summary>
    public class BotMediaStream
    {
        private readonly IAudioSocket audioSocket;
        private readonly IVideoSocket mainVideoSocket;
        private readonly IVideoSocket vbssSocket;
        private readonly List<IVideoSocket> videoSockets;
        private readonly TaskCompletionSource<bool> audioSendStatusActive;
        private readonly TaskCompletionSource<bool> videoSendStatusActive;
        private readonly TaskCompletionSource<bool> startVideoPlayerCompleted;
        private readonly object mLock = new object();
        private readonly ILocalMediaSession mediaSession;
        private AudioVideoFramePlayerSettings audioVideoFramePlayerSettings;
        private AudioVideoFramePlayer audioVideoFramePlayer;
        private long audioTick;
        private long videoTick;
        private long mediaTick;
        private List<AudioMediaBuffer> audioMediaBuffers = new List<AudioMediaBuffer>();
        private List<VideoMediaBuffer> videoMediaBuffers = new List<VideoMediaBuffer>();
        private List<VideoFormat> knownSupportedFormats;
        private int shutdown;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotMediaStream"/> class.
        /// </summary>
        /// <param name="mediaSession">The media session.</param>
        /// <exception cref="InvalidOperationException">Throws when no audio socket is passed in.</exception>
        public BotMediaStream(ILocalMediaSession mediaSession)
        {
            ArgumentVerifier.ThrowOnNullArgument(mediaSession, "mediaSession");

            this.mediaSession = mediaSession;
            this.audioSendStatusActive = new TaskCompletionSource<bool>();
            this.videoSendStatusActive = new TaskCompletionSource<bool>();
            this.startVideoPlayerCompleted = new TaskCompletionSource<bool>();

            this.audioSocket = this.mediaSession.AudioSocket;
            if (this.audioSocket == null)
            {
                throw new InvalidOperationException("A mediaSession needs to have at least an audioSocket");
            }

            this.audioSocket.AudioSendStatusChanged += this.OnAudioSendStatusChanged;

            this.mainVideoSocket = this.mediaSession.VideoSockets?.FirstOrDefault();
            if (this.mainVideoSocket != null)
            {
                this.mainVideoSocket.VideoSendStatusChanged += this.OnVideoSendStatusChanged;
                this.mainVideoSocket.VideoKeyFrameNeeded += this.OnVideoKeyFrameNeeded;
            }

            this.videoSockets = this.mediaSession.VideoSockets?.ToList();

            this.vbssSocket = this.mediaSession.VbssSocket;
            if (this.vbssSocket != null)
            {
                this.vbssSocket.VideoSendStatusChanged += this.OnVbssSocketSendStatusChanged;
            }

            var ignoreTask = this.StartAudioVideoFramePlayerAsync().ForgetAndLogExceptionAsync("Failed to start the player");
        }

        /// <summary>
        /// Subscription for video and vbss.
        /// </summary>
        /// <param name="mediaType">vbss or video.</param>
        /// <param name="mediaSourceId">The video source Id.</param>
        /// <param name="videoResolution">The preferred video resolution.</param>
        /// <param name="socketId">Socket id requesting the video. For vbss it is always 0.</param>
        public void Subscribe(MediaType mediaType, uint mediaSourceId, VideoResolution videoResolution, uint socketId = 0)
        {
            try
            {
                this.ValidateSubscriptionMediaType(mediaType);

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Subscribing to the video source: {mediaSourceId} on socket: {socketId} with the preferred resolution: {videoResolution} and mediaType: {mediaType}");
                if (mediaType == MediaType.Vbss)
                {
                    if (this.vbssSocket == null)
                    {
                        Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"vbss socket not initialized");
                    }
                    else
                    {
                        this.vbssSocket.Subscribe(videoResolution, mediaSourceId);
                    }
                }
                else if (mediaType == MediaType.Video)
                {
                    if (this.videoSockets == null)
                    {
                        Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"video sockets were not created");
                    }
                    else
                    {
                        this.videoSockets[(int)socketId].Subscribe(videoResolution, mediaSourceId);
                    }
                }
            }
            catch (Exception ex)
            {
               Log.Error(new CallerInfo(), LogContext.FrontEnd, $"Video Subscription failed for the socket: {socketId} and MediaSourceId: {mediaSourceId} with exception: {ex}");
            }
        }

        /// <summary>
        /// Unsubscribe to video.
        /// </summary>
        /// <param name="mediaType">vbss or video.</param>
        /// <param name="socketId">Socket id. For vbss it is always 0.</param>
        public void Unsubscribe(MediaType mediaType, uint socketId = 0)
        {
            try
            {
                this.ValidateSubscriptionMediaType(mediaType);

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Unsubscribing to video for the socket: {socketId} and mediaType: {mediaType}");

                if (mediaType == MediaType.Vbss)
                {
                    this.vbssSocket?.Unsubscribe();
                }
                else if (mediaType == MediaType.Video)
                {
                    this.videoSockets[(int)socketId]?.Unsubscribe();
                }
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, $"Unsubscribing to video failed for the socket: {socketId} with exception: {ex}");
            }
        }

        /// <summary>
        /// Shut down.
        /// </summary>
        /// <returns><see cref="Task" />.</returns>
        public async Task ShutdownAsync()
        {
            if (Interlocked.CompareExchange(ref this.shutdown, 1, 1) == 1)
            {
                return;
            }

            await this.startVideoPlayerCompleted.Task.ConfigureAwait(false);

            // unsubscribe
            this.audioVideoFramePlayer.LowOnFrames -= this.OnAudioVideoFramePlayerLowOnFrames;
            if (this.audioSocket != null)
            {
                this.audioSocket.AudioSendStatusChanged -= this.OnAudioSendStatusChanged;
            }

            if (this.mainVideoSocket != null)
            {
                this.mainVideoSocket.VideoKeyFrameNeeded -= this.OnVideoKeyFrameNeeded;
                this.mainVideoSocket.VideoSendStatusChanged -= this.OnVideoSendStatusChanged;
            }

            if (this.vbssSocket != null)
            {
                this.vbssSocket.VideoSendStatusChanged -= this.OnVbssSocketSendStatusChanged;
            }

            await this.audioVideoFramePlayer.ShutdownAsync().ConfigureAwait(false);

            // make sure all the audio and video buffers are disposed, it can happen that,
            // the buffers were not enqueued but the call was disposed if the caller hangs up quickly
            foreach (var audioMediaBuffer in this.audioMediaBuffers)
            {
                audioMediaBuffer.Dispose();
            }

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"disposed {this.audioMediaBuffers.Count} audioMediaBUffers.");
            foreach (var videoMediaBuffer in this.videoMediaBuffers)
            {
                videoMediaBuffer.Dispose();
            }

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"disposed {this.videoMediaBuffers.Count} videoMediaBuffers");
            this.audioMediaBuffers.Clear();
            this.videoMediaBuffers.Clear();
        }

        /// <summary>
        /// Ensure media type is video or VBSS.
        /// </summary>
        /// <param name="mediaType">Media type to validate.</param>
        private void ValidateSubscriptionMediaType(MediaType mediaType)
        {
            if (mediaType != MediaType.Vbss && mediaType != MediaType.Video)
            {
                throw new ArgumentOutOfRangeException($"Invalid mediaType: {mediaType}");
            }
        }

        /// <summary>
        /// Event to signal the player is low on frames.
        /// </summary>
        /// <param name="sender">The video socket.</param>
        /// <param name="e">Event containing media type and length of media remaining.</param>
        private void OnAudioVideoFramePlayerLowOnFrames(object sender, LowOnFramesEventArgs e)
        {
            if (this.shutdown != 1)
            {
                Log.Info(new CallerInfo(), LogContext.Media, $"Low on frames event raised for {e.MediaType}, remaining lenght is {e.RemainingMediaLengthInMS} ms");

                // here we want to keep the AV creation in sync so we take as reference audio.
                if (e.MediaType == MediaType.Audio)
                {
                    // use the past tick as reference to avoid av out of sync
                    this.CreateAVBuffers(this.mediaTick, replayed: true);
                    this.audioVideoFramePlayer?.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ForgetAndLogExceptionAsync("Failed to enqueue AV buffers");

                    Log.Info(new CallerInfo(), LogContext.Media, $"Low on audio event raised, enqueued {this.audioMediaBuffers.Count} buffers last audio tick {this.audioTick} and mediatick {this.mediaTick}");
                }

                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "enqueued more frames in the audioVideoPlayer");
            }
        }

        /// <summary>
        /// Initialize AV frame player.
        /// </summary>
        /// <returns>Task denoting creation of the player with initial frames enqueued.</returns>
        private async Task StartAudioVideoFramePlayerAsync()
        {
            try
            {
                await Task.WhenAll(this.audioSendStatusActive.Task, this.videoSendStatusActive.Task).ConfigureAwait(false);

                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "Send status active for audio and video Creating the audio video player");
                this.audioVideoFramePlayerSettings =
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
                this.audioVideoFramePlayer = new AudioVideoFramePlayer(
                    (AudioSocket)this.audioSocket,
                    (VideoSocket)this.mainVideoSocket,
                    this.audioVideoFramePlayerSettings);

                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "created the audio video player");

                this.audioVideoFramePlayer.LowOnFrames += this.OnAudioVideoFramePlayerLowOnFrames;

                // Create AV buffers
                var currentTick = DateTime.Now.Ticks;
                this.CreateAVBuffers(currentTick, replayed: false);

                await this.audioVideoFramePlayer.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.Media, "Failed to create the audioVideoFramePlayer with exception {0}", ex);
            }
            finally
            {
                this.startVideoPlayerCompleted.TrySetResult(true);
            }
        }

        /// <summary>
        /// Callback for informational updates from the media plaform about audio status changes.
        /// Once the status becomes active, audio can be loopbacked.
        /// </summary>
        /// <param name="sender">The audio socket.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAudioSendStatusChanged(object sender, AudioSendStatusChangedEventArgs e)
        {
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[AudioSendStatusChangedEventArgs(MediaSendStatus={0})]",
                e.MediaSendStatus);

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                this.audioSendStatusActive.SetResult(true);
            }
        }

        /// <summary>
        /// Callback for informational updates from the media plaform about video status changes.
        /// Once the Status becomes active, then video can be sent.
        /// </summary>
        /// <param name="sender">The video socket.</param>
        /// <param name="e">Event arguments.</param>
        private void OnVideoSendStatusChanged(object sender, VideoSendStatusChangedEventArgs e)
        {
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[VideoSendStatusChangedEventArgs(MediaSendStatus=<{0}>]",
                e.MediaSendStatus);

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[VideoSendStatusChangedEventArgs(MediaSendStatus=<{0}>;PreferredVideoSourceFormat=<{1}>]",
                e.MediaSendStatus,
                string.Join(";", e.PreferredEncodedVideoSourceFormats.ToList()));

                var previousSupportedFormats = (this.knownSupportedFormats != null && this.knownSupportedFormats.Any()) ? this.knownSupportedFormats :
                   new List<VideoFormat>();
                this.knownSupportedFormats = e.PreferredEncodedVideoSourceFormats.ToList();

                // when this is false it means that we have received a new event with different videoFormats
                // the behavior for this bot is to clean up the previous enqueued media and push the new formats,
                // starting from beginning
                if (!this.videoSendStatusActive.TrySetResult(true))
                {
                    if (this.knownSupportedFormats != null && this.knownSupportedFormats.Any() &&

                        // here it means we got a new video fromat so we need to restart the player
                        this.knownSupportedFormats.Select(x => x.GetId()).Except(previousSupportedFormats.Select(y => y.GetId())).Any())
                    {
                        // we restart the player
                        this.audioVideoFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync();

                        Log.Info(
                            new CallerInfo(),
                            LogContext.Media,
                            "[VideoSendStatusChangedEventArgs(MediaSendStatus=<{0}> enqueuing new formats: {1}]",
                            e.MediaSendStatus,
                            string.Join(";", this.knownSupportedFormats));

                        // Create the AV buffers
                        var currentTick = DateTime.Now.Ticks;
                        this.CreateAVBuffers(currentTick, replayed: false);

                        this.audioVideoFramePlayer?.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ForgetAndLogExceptionAsync();
                    }
                }
            }
            else if (e.MediaSendStatus == MediaSendStatus.Inactive)
            {
                if (this.videoSendStatusActive.Task.IsCompleted && this.audioVideoFramePlayer != null)
                {
                    this.audioVideoFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync();
                }
            }
        }

        /// <summary>
        /// If the application has configured the VideoSocket to receive encoded media, this
        /// event is raised each time a key frame is needed. Events are serialized, so only
        /// one event at a time is raised to the app.
        /// </summary>
        /// <param name="sender">Video socket.</param>
        /// <param name="e">Event args specifying the socket id, media type and video formats for which key frame is being requested.</param>
        private void OnVideoKeyFrameNeeded(object sender, VideoKeyFrameNeededEventArgs e)
        {
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[VideoKeyFrameNeededEventArgs(MediaType=<{{0}}>;SocketId=<{{1}}>" + "VideoFormats=<{2}>] calling RequestKeyFrame on the videoSocket",
                e.MediaType,
                e.SocketId,
                string.Join(";", e.VideoFormats.ToList()));
        }

        /// <summary>
        /// Performs action when the vbss socket send status changed event is received.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The video send status changed event arguments.
        /// </param>
        private void OnVbssSocketSendStatusChanged(object sender, VideoSendStatusChangedEventArgs e)
        {
            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                var currentTick = DateTime.Now.Ticks;
                var videoBuffers = Utilities.CreateVideoMediaBuffers(currentTick, e.PreferredEncodedVideoSourceFormats?.ToList(), replayed: false);
                if (videoBuffers.Any())
                {
                    try
                    {
                        foreach (var buffer in videoBuffers)
                        {
                            this.vbssSocket.Send(buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            new CallerInfo(),
                            LogContext.Media,
                            $"Exception in sending video buffer.{0}",
                            ex);
                    }

                    try
                    {
                        foreach (var buffer in videoBuffers)
                        {
                            buffer.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            new CallerInfo(),
                            LogContext.Media,
                            $"Exception in disposing video buffer.{0}",
                            ex);
                    }
                }
            }
        }

        /// <summary>
        /// Create audio video buffers.
        /// </summary>
        /// <param name="referenceTick">Current clock tick.</param>
        /// <param name="replayed">If frame is replayed.</param>
        private void CreateAVBuffers(long referenceTick, bool replayed)
        {
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "Creating AudioVideoBuffers");

            lock (this.mLock)
            {
                this.videoMediaBuffers = Utilities.CreateVideoMediaBuffers(
                    referenceTick,
                    this.knownSupportedFormats,
                    replayed);

                this.audioMediaBuffers = Utilities.CreateAudioMediaBuffers(
                    referenceTick,
                    replayed);

                // update the tick for next iteration
                this.audioTick = this.audioMediaBuffers.Last().Timestamp;
                this.videoTick = this.videoMediaBuffers.Last().Timestamp;
                this.mediaTick = Math.Max(this.audioTick, this.videoTick);
            }
        }
    }
}
