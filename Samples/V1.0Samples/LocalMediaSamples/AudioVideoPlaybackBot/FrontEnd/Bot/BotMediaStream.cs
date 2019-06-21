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
    using Microsoft.Graph.Communications.Calls.Media;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.Skype.Internal.Media.Services.Common;
    using Sample.Common;

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
        private readonly IGraphLogger logger;
        private AudioVideoFramePlayerSettings audioVideoFramePlayerSettings;
        private AudioVideoFramePlayer audioVideoFramePlayer;
        private AudioVideoFramePlayer vbssFramePlayer;
        private long audioTick;
        private long videoTick;
        private long mediaTick;
        private List<AudioMediaBuffer> audioMediaBuffers = new List<AudioMediaBuffer>();
        private List<VideoMediaBuffer> videoMediaBuffers = new List<VideoMediaBuffer>();
        private List<VideoMediaBuffer> vbssMediaBuffers = new List<VideoMediaBuffer>();
        private List<VideoFormat> videoKnownSupportedFormats;
        private List<VideoFormat> vbssKnownSupportedFormats;
        private int shutdown;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotMediaStream"/> class.
        /// </summary>
        /// <param name="mediaSession">The media session.</param>
        /// <param name="logger">Graph logger.</param>
        /// <exception cref="InvalidOperationException">Throws when no audio socket is passed in.</exception>
        public BotMediaStream(ILocalMediaSession mediaSession, IGraphLogger logger)
        {
            ArgumentVerifier.ThrowOnNullArgument(mediaSession, "mediaSession");

            this.mediaSession = mediaSession;
            this.logger = logger;
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

            var ignoreTask = this.StartAudioVideoFramePlayerAsync().ForgetAndLogExceptionAsync(this.logger, "Failed to start the player");
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

                this.logger.Info($"Subscribing to the video source: {mediaSourceId} on socket: {socketId} with the preferred resolution: {videoResolution} and mediaType: {mediaType}");
                if (mediaType == MediaType.Vbss)
                {
                    if (this.vbssSocket == null)
                    {
                        this.logger.Warn($"vbss socket not initialized");
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
                        this.logger.Warn($"video sockets were not created");
                    }
                    else
                    {
                        this.videoSockets[(int)socketId].Subscribe(videoResolution, mediaSourceId);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Video Subscription failed for the socket: {socketId} and MediaSourceId: {mediaSourceId} with exception");
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

                this.logger.Info($"Unsubscribing to video for the socket: {socketId} and mediaType: {mediaType}");

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
                this.logger.Error(ex, $"Unsubscribing to video failed for the socket: {socketId} with exception");
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
            if (this.vbssFramePlayer != null)
            {
                this.vbssFramePlayer.LowOnFrames -= this.OnVbssPlayerLowOnFrames;
            }

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

            // shutting down the players
            if (this.audioVideoFramePlayer != null)
            {
                await this.audioVideoFramePlayer.ShutdownAsync().ConfigureAwait(false);
            }

            if (this.vbssFramePlayer != null)
            {
                await this.vbssFramePlayer.ShutdownAsync().ConfigureAwait(false);
            }

            // make sure all the audio and video buffers are disposed, it can happen that,
            // the buffers were not enqueued but the call was disposed if the caller hangs up quickly
            foreach (var audioMediaBuffer in this.audioMediaBuffers)
            {
                audioMediaBuffer.Dispose();
            }

            this.logger.Info($"disposed {this.audioMediaBuffers.Count} audioMediaBUffers.");
            foreach (var videoMediaBuffer in this.videoMediaBuffers)
            {
                videoMediaBuffer.Dispose();
            }

            this.logger.Info($"disposed {this.videoMediaBuffers.Count} videoMediaBuffers");
            foreach (var videoMediaBuffer in this.vbssMediaBuffers)
            {
                videoMediaBuffer.Dispose();
            }

            this.logger.Info($"disposed {this.vbssMediaBuffers.Count} vbssMediaBuffers");
            this.audioMediaBuffers.Clear();
            this.videoMediaBuffers.Clear();
            this.vbssMediaBuffers.Clear();
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
                this.logger.Info($"Low on frames event raised for {e.MediaType}, remaining lenght is {e.RemainingMediaLengthInMS} ms");

                // here we want to keep the AV creation in sync so we take as reference audio.
                if (e.MediaType == MediaType.Audio)
                {
                    // use the past tick as reference to avoid av out of sync
                    this.CreateAVBuffers(this.mediaTick, replayed: true);
                    this.audioVideoFramePlayer?.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ForgetAndLogExceptionAsync(this.logger, "Failed to enqueue AV buffers");

                    this.logger.Info($"Low on audio event raised, enqueued {this.audioMediaBuffers.Count} buffers last audio tick {this.audioTick} and mediatick {this.mediaTick}");
                }

                this.logger.Info("enqueued more frames in the audioVideoPlayer");
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

                this.logger.Info("Send status active for audio and video Creating the audio video player");
                this.audioVideoFramePlayerSettings =
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
                this.audioVideoFramePlayer = new AudioVideoFramePlayer(
                    (AudioSocket)this.audioSocket,
                    (VideoSocket)this.mainVideoSocket,
                    this.audioVideoFramePlayerSettings);

                this.logger.Info("created the audio video player");

                this.audioVideoFramePlayer.LowOnFrames += this.OnAudioVideoFramePlayerLowOnFrames;

                // Create AV buffers
                var currentTick = DateTime.Now.Ticks;
                this.CreateAVBuffers(currentTick, replayed: false);

                await this.audioVideoFramePlayer.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to create the audioVideoFramePlayer with exception");
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
            this.logger.Info($"[AudioSendStatusChangedEventArgs(MediaSendStatus={e.MediaSendStatus})]");

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                this.audioSendStatusActive.TrySetResult(true);
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
            this.logger.Info($"[VideoSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>]");

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                this.logger.Info($"[VideoSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>;PreferredVideoSourceFormat=<{string.Join(";", e.PreferredEncodedVideoSourceFormats.ToList())}>]");

                var previousSupportedFormats = (this.videoKnownSupportedFormats != null && this.videoKnownSupportedFormats.Any()) ? this.videoKnownSupportedFormats :
                   new List<VideoFormat>();
                this.videoKnownSupportedFormats = e.PreferredEncodedVideoSourceFormats.ToList();

                // when this is false it means that we have received a new event with different videoFormats
                // the behavior for this bot is to clean up the previous enqueued media and push the new formats,
                // starting from beginning
                if (!this.videoSendStatusActive.TrySetResult(true))
                {
                    if (this.videoKnownSupportedFormats != null && this.videoKnownSupportedFormats.Any() &&

                        // here it means we got a new video fromat so we need to restart the player
                        this.videoKnownSupportedFormats.Select(x => x.GetId()).Except(previousSupportedFormats.Select(y => y.GetId())).Any())
                    {
                        // we restart the player
                        this.audioVideoFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(this.logger);

                        this.logger.Info($"[VideoSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}> enqueuing new formats: {string.Join(";", this.videoKnownSupportedFormats)}]");

                        // Create the AV buffers
                        var currentTick = DateTime.Now.Ticks;
                        this.CreateAVBuffers(currentTick, replayed: false);

                        this.audioVideoFramePlayer?.EnqueueBuffersAsync(this.audioMediaBuffers, this.videoMediaBuffers).ForgetAndLogExceptionAsync(this.logger);
                    }
                }
            }
            else if (e.MediaSendStatus == MediaSendStatus.Inactive)
            {
                if (this.videoSendStatusActive.Task.IsCompleted && this.audioVideoFramePlayer != null)
                {
                    this.audioVideoFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(this.logger);
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
            this.logger.Info($"[VideoKeyFrameNeededEventArgs(MediaType=<{{e.MediaType}}>;SocketId=<{{e.SocketId}}>" +
                             $"VideoFormats=<{string.Join(";", e.VideoFormats.ToList())}>] calling RequestKeyFrame on the videoSocket");
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
            this.logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>]");

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                this.logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}>;PreferredVideoSourceFormat=<{string.Join(";", e.PreferredEncodedVideoSourceFormats.ToList())}>]");

                var previousSupportedFormats = (this.vbssKnownSupportedFormats != null && this.vbssKnownSupportedFormats.Any()) ? this.vbssKnownSupportedFormats :
                   new List<VideoFormat>();
                this.vbssKnownSupportedFormats = e.PreferredEncodedVideoSourceFormats.ToList();

                if (this.vbssFramePlayer == null)
                {
                    this.CreateVbssFramePlayer();
                }

                // when this is false it means that we have received a new event with different videoFormats
                // the behavior for this bot is to clean up the previous enqueued media and push the new formats,
                // starting from beginning
                else
                {
                    // we restart the player
                    this.vbssFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(this.logger);
                }

                // enqueue video buffers
                this.logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus}> enqueuing new formats: {string.Join(";", this.vbssKnownSupportedFormats)}]");

                // Create the video buffers
                this.vbssMediaBuffers = Utilities.CreateVideoMediaBuffers(DateTime.Now.Ticks, this.vbssKnownSupportedFormats, true, this.logger);
                this.vbssFramePlayer?.EnqueueBuffersAsync(new List<AudioMediaBuffer>(), this.vbssMediaBuffers).ForgetAndLogExceptionAsync(this.logger);
            }
            else if (e.MediaSendStatus == MediaSendStatus.Inactive)
            {
                this.vbssFramePlayer?.ClearAsync().ForgetAndLogExceptionAsync(this.logger);
            }
        }

        /// <summary>
        /// Callback handler for the lowOnFrames event that the vbss frame player will raise when there are no more frames to stream.
        /// The behavior is to enqueue more frames.
        /// </summary>
        /// <param name="sender">The vbss frame player.</param>
        /// <param name="e">LowOnframes eventArgs.</param>
        private void OnVbssPlayerLowOnFrames(object sender, LowOnFramesEventArgs e)
        {
            if (this.shutdown != 1)
            {
                this.logger.Info($"Low on frames event raised for the vbss player, remaining lenght is {e.RemainingMediaLengthInMS} ms");

                // Create the video buffers
                this.vbssMediaBuffers = Utilities.CreateVideoMediaBuffers(DateTime.Now.Ticks, this.vbssKnownSupportedFormats, true, this.logger);
                this.vbssFramePlayer?.EnqueueBuffersAsync(new List<AudioMediaBuffer>(), this.vbssMediaBuffers).ForgetAndLogExceptionAsync(this.logger);
                this.logger.Info("enqueued more frames in the vbssFramePlayer");
            }
        }

        /// <summary>
        /// Create audio video buffers.
        /// </summary>
        /// <param name="referenceTick">Current clock tick.</param>
        /// <param name="replayed">If frame is replayed.</param>
        private void CreateAVBuffers(long referenceTick, bool replayed)
        {
            this.logger.Info("Creating AudioVideoBuffers");

            lock (this.mLock)
            {
                this.videoMediaBuffers = Utilities.CreateVideoMediaBuffers(
                    referenceTick,
                    this.videoKnownSupportedFormats,
                    replayed,
                    this.logger);

                this.audioMediaBuffers = Utilities.CreateAudioMediaBuffers(
                    referenceTick,
                    replayed,
                    this.logger);

                // update the tick for next iteration
                this.audioTick = this.audioMediaBuffers.Last().Timestamp;
                this.videoTick = this.videoMediaBuffers.Last().Timestamp;
                this.mediaTick = Math.Max(this.audioTick, this.videoTick);
            }
        }

        /// <summary>
        /// Creates the vbss player that will stream the video buffers for the sharer.
        /// </summary>
        private void CreateVbssFramePlayer()
        {
            try
            {
                this.logger.Info("Creating the vbss FramePlayer");
                this.audioVideoFramePlayerSettings =
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
                this.vbssFramePlayer = new AudioVideoFramePlayer(
                    null,
                    (VideoSocket)this.vbssSocket,
                    this.audioVideoFramePlayerSettings);

                this.vbssFramePlayer.LowOnFrames += this.OnVbssPlayerLowOnFrames;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Failed to create the vbssFramePlayer with exception {ex}");
            }
        }
    }
}
