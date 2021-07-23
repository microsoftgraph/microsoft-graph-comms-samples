// <copyright file="BotMediaStream.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using Microsoft.Skype.Internal.Media.Services.Common;
using System;
using System.Collections.Generic;
using PsiBot.Service.Settings;
using System.Linq;
using System.Threading;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.TeamsBot;
using Microsoft.Psi.Data;
using System.Runtime.InteropServices;

namespace PsiBot.Services.Bot
{
    /// <summary>
    /// Class responsible for streaming audio and video.
    /// </summary>
    public class BotMediaStream : ObjectRootDisposable
    {

        /// <summary>
        /// Contains a map from simple color/width/height combinations to VideoFormat objects.
        /// </summary>
        public static readonly Dictionary<(VideoColorFormat format, int width, int height), VideoFormat> VideoFormatMap = new Dictionary<(VideoColorFormat format, int width, int height), VideoFormat>()
        {
            { (VideoColorFormat.NV12, 1920, 1080), VideoFormat.NV12_1920x1080_15Fps },
            { (VideoColorFormat.NV12, 1280, 720), VideoFormat.NV12_1280x720_15Fps },
            { (VideoColorFormat.NV12, 640, 360), VideoFormat.NV12_640x360_15Fps },
            { (VideoColorFormat.NV12, 480, 270), VideoFormat.NV12_480x270_15Fps },
            { (VideoColorFormat.NV12, 424, 240), VideoFormat.NV12_424x240_15Fps },
            { (VideoColorFormat.NV12, 360, 640), VideoFormat.NV12_360x640_15Fps },
            { (VideoColorFormat.NV12, 320, 180), VideoFormat.NV12_320x180_15Fps },
            { (VideoColorFormat.NV12, 270, 480), VideoFormat.NV12_270x480_15Fps },
            { (VideoColorFormat.NV12, 240, 424), VideoFormat.NV12_240x424_15Fps },
            { (VideoColorFormat.NV12, 180, 320), VideoFormat.NV12_180x320_30Fps },
        };

        /// <summary>
        /// Contains a map from simple width/height combinations to VideoFormat objects.
        /// </summary>
        public static readonly Dictionary<(int width, int height), VideoFormat> VideoSizeMap = new Dictionary<(int width, int height), VideoFormat>()
        {
            { (1920, 1080), VideoFormat.NV12_1920x1080_15Fps },
            { (1280, 720), VideoFormat.NV12_1280x720_15Fps },
            { (640, 360), VideoFormat.NV12_640x360_15Fps },
            { (480, 270), VideoFormat.NV12_480x270_15Fps },
            { (424, 240), VideoFormat.NV12_424x240_15Fps },
            { (360, 640), VideoFormat.NV12_360x640_15Fps },
            { (320, 180), VideoFormat.NV12_320x180_15Fps },
            { (270, 480), VideoFormat.NV12_270x480_15Fps },
            { (240, 424), VideoFormat.NV12_240x424_15Fps },
            { (180, 320), VideoFormat.NV12_180x320_30Fps },
        };

        /// <summary>
        /// The participants
        /// </summary>
        internal List<IParticipant> participants;

        private readonly IAudioSocket audioSocket;
        private readonly IVideoSocket vbssSocket;
        private readonly IVideoSocket mainVideoSocket;

        private readonly List<IVideoSocket> multiViewVideoSockets;

        private readonly ILocalMediaSession mediaSession;
        private readonly IGraphLogger logger;
        private readonly MediaFrameSourceComponent mediaFrameSourceComponent;
        private int shutdown;
        private MediaSendStatus videoMediaSendStatus = MediaSendStatus.Inactive;
        private MediaSendStatus vbssMediaSendStatus = MediaSendStatus.Inactive;
        private MediaSendStatus audioSendStatus = MediaSendStatus.Inactive;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotMediaStream"/> class.
        /// </summary>
        /// <param name="mediaSession">The media session.</param>
        /// <param name="callHandler">Call handler.</param>
        /// <param name="pipeline">Psi Pipeline.</param>
        /// <param name="teamsBot">Teams bot instance.</param>
        /// <param name="exporter">Psi Exporter.</param>
        /// <param name="logger">Graph logger.</param>
        /// <param name="botConfiguration">Bot configuration</param>
        /// <exception cref="InvalidOperationException">A mediaSession needs to have at least an audioSocket</exception>
        public BotMediaStream(
            ILocalMediaSession mediaSession,
            CallHandler callHandler,
            Pipeline pipeline, 
            ITeamsBot teamsBot, 
            Exporter exporter,
            IGraphLogger logger,
            BotConfiguration botConfiguration
        )
            : base(logger)
        {
            ArgumentVerifier.ThrowOnNullArgument(mediaSession, nameof(mediaSession));
            ArgumentVerifier.ThrowOnNullArgument(logger, nameof(logger));
            ArgumentVerifier.ThrowOnNullArgument(botConfiguration, nameof(botConfiguration));

            this.mediaSession = mediaSession;
            this.logger = logger;

            this.participants = new List<IParticipant>();

            this.mediaFrameSourceComponent = new MediaFrameSourceComponent(pipeline, callHandler, this.logger);

            if (exporter != null)
            {
                this.mediaFrameSourceComponent.Audio.Parallel(
                    (id, stream) =>
                    {
                        // Extract and persist audio streams with the original timestamps for each buffer
                        stream.Process<(AudioBuffer, DateTime), AudioBuffer>((tuple, _, emitter) =>
                        {
                            (var audioBuffer, var originatingTime) = tuple;
                            if (originatingTime > emitter.LastEnvelope.OriginatingTime)
                            {
                                // Out-of-order messages are ignored
                                emitter.Post(audioBuffer, originatingTime);
                            }
                        }).Write($"Participants.{id}.Audio", exporter);
                    },
                    branchTerminationPolicy: BranchTerminationPolicy<string, (AudioBuffer, DateTime)>.Never(),
                    name: "PersistParticipantAudio");

                this.mediaFrameSourceComponent.Video.Parallel(
                    (id, stream) =>
                    {
                        // Extract and persist video streams with the original timestamps for each image (also encode to jpeg)
                        stream.Process<(Shared<Image>, DateTime), Shared<Image>>(
                            (tuple, _, emitter) =>
                            {
                                (var image, var originatingTime) = tuple;
                                if (originatingTime > emitter.LastEnvelope.OriginatingTime)
                                {
                                    // Out-of-order messages are ignored
                                    emitter.Post(image, originatingTime);
                                }
                            },
                            DeliveryPolicy.LatestMessage).EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write($"Participants.{id}.Video", exporter, true);
                    },
                    DeliveryPolicy.LatestMessage,
                    BranchTerminationPolicy<string, (Shared<Image>, DateTime)>.Never(),
                    name: "PersistParticipantVideo");

                teamsBot.ScreenShareOut?.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("Bot.Screen", exporter, true);
                teamsBot.AudioOut?.Write("Bot.Audio", exporter);
                teamsBot.VideoOut?.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Write("Bot.Video", exporter, true);
            }
            
            this.mediaFrameSourceComponent.Audio.PipeTo(teamsBot.AudioIn);
            this.mediaFrameSourceComponent.Video.PipeTo(teamsBot.VideoIn);
            teamsBot.AudioOut?.Do(buffer =>
            {
                if (this.audioSendStatus == MediaSendStatus.Active && teamsBot.EnableAudioOutput)
                {
                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(buffer.Length);
                    Marshal.Copy(buffer.Data, 0, unmanagedPointer, buffer.Length);
                    this.SendAudio(new AudioSendBuffer(unmanagedPointer, buffer.Length, AudioFormat.Pcm16K));
                    Marshal.FreeHGlobal(unmanagedPointer);
                }
            });

            teamsBot.VideoOut?.Do(
                frame =>
                {
                    if (this.videoMediaSendStatus == MediaSendStatus.Active && teamsBot.EnableVideoOutput)
                    {
                        var image = frame.Resource;
                        var nv12 = this.BGRAtoNV12(image.ImageData, image.Width, image.Height);
                        var format = VideoFormatMap[(VideoColorFormat.NV12, teamsBot.VideoSize.Width, teamsBot.VideoSize.Height)];
                        this.SendVideo(new VideoSendBuffer(nv12, (uint)nv12.Length, format));
                    }
                },
                DeliveryPolicy.LatestMessage);

            teamsBot.ScreenShareOut?.Do(
                frame =>
                {
                    if (this.vbssMediaSendStatus == MediaSendStatus.Active && teamsBot.EnableScreenSharing)
                    {
                        var image = frame.Resource;
                        var nv12 = this.BGRAtoNV12(image.ImageData, image.Width, image.Height);
                        var format = VideoFormatMap[(VideoColorFormat.NV12, teamsBot.ScreenShareSize.Width, teamsBot.ScreenShareSize.Height)];
                        this.SendScreen(new VideoSendBuffer(nv12, (uint)nv12.Length, format));
                    }
                },
                DeliveryPolicy.LatestMessage);

            // Subscribe to the audio media.
            this.audioSocket = this.mediaSession.AudioSocket;
            if (this.audioSocket == null)
            {
                throw new InvalidOperationException("A mediaSession needs to have at least an audioSocket");
            }

            this.audioSocket.AudioSendStatusChanged += this.OnAudioSendStatusChanged;
            this.audioSocket.AudioMediaReceived += this.OnAudioMediaReceived;
            this.mainVideoSocket = this.mediaSession.VideoSockets?.FirstOrDefault();
            if (this.mainVideoSocket != null)
            {
                this.mainVideoSocket.VideoSendStatusChanged += this.OnVideoSendStatusChanged;
                this.mainVideoSocket.VideoKeyFrameNeeded += this.OnVideoKeyFrameNeeded;
                this.mainVideoSocket.VideoMediaReceived += this.OnVideoMediaReceived;
            }

            this.multiViewVideoSockets = this.mediaSession.VideoSockets?.ToList();
            foreach (var videoSocket in this.multiViewVideoSockets)
            {
                videoSocket.VideoMediaReceived += this.OnVideoMediaReceived;
                videoSocket.VideoReceiveStatusChanged += this.OnVideoReceiveStatusChanged;
            }

            this.vbssSocket = this.mediaSession.VbssSocket;
            if (this.vbssSocket != null)
            {
                this.vbssSocket.VideoSendStatusChanged += this.OnVbssSocketSendStatusChanged;
                this.vbssSocket.MediaStreamFailure += this.OnVbssMediaStreamFailure;
            }
        }

        /// <summary>
        /// Gets the participants.
        /// </summary>
        /// <returns>List&lt;IParticipant&gt;.</returns>
        public List<IParticipant> GetParticipants()
        {
            return participants;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            // Event Dispose of the bot media stream object
            base.Dispose(disposing);

            if (Interlocked.CompareExchange(ref this.shutdown, 1, 1) == 1)
            {
                return;
            }

            if (this.audioSocket != null)
            {
                this.audioSocket.AudioSendStatusChanged -= this.OnAudioSendStatusChanged;
                this.audioSocket.AudioMediaReceived -= this.OnAudioMediaReceived;
            }

            if (this.mainVideoSocket != null)
            {
                this.mainVideoSocket.VideoKeyFrameNeeded -= this.OnVideoKeyFrameNeeded;
                this.mainVideoSocket.VideoSendStatusChanged -= this.OnVideoSendStatusChanged;
                this.mainVideoSocket.VideoMediaReceived -= this.OnVideoMediaReceived;
            }

            if (this.vbssSocket != null)
            {
                this.vbssSocket.VideoSendStatusChanged -= this.OnVbssSocketSendStatusChanged;
            }
        }

        /// <summary>
        /// Convert BGRA image to NV12.
        /// </summary>
        /// <param name="data">BGRA data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <returns>NV12 encoded bytes.</returns>
        private unsafe byte[] BGRAtoNV12(IntPtr data, int width, int height)
        {
            var bytes = (byte*)data.ToPointer();
            byte[] result = new byte[(int)(1.5 * (width * height))];

            // https://www.fourcc.org/fccyvrgb.php
            for (var i = 0; i < width * height; i++)
            {
                var p = bytes + (i * 4);
                var b = *p;
                var g = *(p + 1);
                var r = *(p + 2);
                var y = (byte)Math.Max(0, Math.Min(255, (0.257 * r) + (0.504 * g) + (0.098 * b) + 16));
                result[i] = y;
            }

            var stride = width * 4;
            var uv = width * height;
            for (var j = 0; j < height; j += 2)
            {
                for (var i = 0; i < width; i += 2)
                {
                    var p = bytes + (i * 4) + (j * width * 4);
                    var b = (*p + *(p + 4) + *(p + stride) + *(p + stride + 4)) / 4;
                    var g = (*(p + 1) + *(p + 5) + *(p + stride + 1) + *(p + stride + 5)) / 4;
                    var r = (*(p + 2) + *(p + 6) + *(p + stride + 2) + *(p + stride + 6)) / 4;
                    var u = (byte)Math.Max(0, Math.Min(255, -(0.148 * r) - (0.291 * g) + (0.439 * b) + 128));
                    var v = (byte)Math.Max(0, Math.Min(255, (0.439 * r) - (0.368 * g) - (0.071 * b) + 128));
                    result[uv++] = u;
                    result[uv++] = v;
                }
            }

            return result;
        }

        #region Subscription
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
                    if (this.multiViewVideoSockets == null)
                    {
                        this.logger.Warn($"video sockets were not created");
                    }
                    else
                    {
                        this.multiViewVideoSockets[(int)socketId].Subscribe(videoResolution, mediaSourceId);
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
                    this.multiViewVideoSockets[(int)socketId]?.Unsubscribe();
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"Unsubscribing to video failed for the socket: {socketId} with exception");
            }
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
        #endregion

        #region Audio
        /// <summary>
        /// Callback for informational updates from the media plaform about audio status changes.
        /// Once the status becomes active, audio can be loopbacked.
        /// </summary>
        /// <param name="sender">The audio socket.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAudioSendStatusChanged(object sender, AudioSendStatusChangedEventArgs e)
        {
            this.logger.Info($"[AudioSendStatusChangedEventArgs(MediaSendStatus={e.MediaSendStatus})]");
            this.audioSendStatus = e.MediaSendStatus;
        }

        /// <summary>
        /// Receive audio from subscribed participant.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The audio media received arguments.</param>
        private void OnAudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
        {
            try
            {
                this.mediaFrameSourceComponent.Received(e.Buffer);
                e.Buffer.Dispose();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex);
            }
            finally
            {
                e.Buffer.Dispose();
            }

        }

        /// <summary>
        /// Sends an <see cref="AudioMediaBuffer"/> to the call from the Bot's audio feed.
        /// </summary>
        /// <param name="buffer">The audio buffer to send.</param>
        private void SendAudio(AudioMediaBuffer buffer)
        {
            // Send the audio to our outgoing video stream
            try
            {
                this.audioSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"[OnAudioReceived] Exception while calling audioSocket.Send()");
            }
        }
        #endregion

        #region Video
        /// <summary>
        /// Callback for informational updates from the media plaform about video status changes.
        /// Once the Status becomes active, then video can be sent.
        /// </summary>
        /// <param name="sender">The video socket.</param>
        /// <param name="e">Event arguments.</param>
        private void OnVideoSendStatusChanged(object sender, VideoSendStatusChangedEventArgs e)
        {
            this.logger.Info($"[VideoSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus};PreferredVideoSourceFormat=<{e.PreferredVideoSourceFormat}>]");
            this.videoMediaSendStatus = e.MediaSendStatus;
        }

        /// <summary>
        /// Callback for informational updates from the media plaform about video status changes.
        /// Once the Status becomes active, then video can be received.
        /// </summary>
        /// <param name="sender">The video socket.</param>
        /// <param name="e">Event arguments.</param>
        private void OnVideoReceiveStatusChanged(object sender, VideoReceiveStatusChangedEventArgs e)
        {
            this.logger.Info($"[VideoReceiveStatusChangedEventArgs(MediaReceiveStatus=<{e.MediaReceiveStatus}>]");
        }

        /// <summary>
        /// Receive video from subscribed participant.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The video media received arguments.</param>
        private void OnVideoMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            this.logger.Info($"[VideoMediaReceivedEventArgs(Data=<{e.Buffer.Data.ToString()}>, Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp}, Width={e.Buffer.VideoFormat.Width}, Height={e.Buffer.VideoFormat.Height}, ColorFormat={e.Buffer.VideoFormat.VideoColorFormat}, FrameRate={e.Buffer.VideoFormat.FrameRate} MediaSourceId={e.Buffer.MediaSourceId})]");
            this.mediaFrameSourceComponent.Received(e.Buffer, e.Buffer.MediaSourceId);
            e.Buffer.Dispose();
        }

        /// <summary>
        /// Sends a <see cref="VideoMediaBuffer"/> to the call from the Bot's video feed.
        /// </summary>
        /// <param name="buffer">The video buffer to send.</param>
        private void SendVideo(VideoMediaBuffer buffer)
        {
            // Send the video to our outgoing video stream
            try
            {
                this.mainVideoSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"[OnVideoMediaReceived] Exception while calling mainVideoSocket.Send()");
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
        #endregion

        #region Screen Share
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
            this.logger.Info($"[VbssSendStatusChangedEventArgs(MediaSendStatus=<{e.MediaSendStatus};PreferredVideoSourceFormat=<{e.PreferredVideoSourceFormat}>]");
            this.vbssMediaSendStatus = e.MediaSendStatus;
        }

        /// <summary>
        /// Called upon VBSS media stream failure.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnVbssMediaStreamFailure(object sender, MediaStreamFailureEventArgs e)
        {
            this.logger.Error($"[VbssOnMediaStreamFailure({e})]");
        }

        /// <summary>
        /// Sends a <see cref="VideoMediaBuffer"/> as a shared screen frame to the call from the Bot's video feed.
        /// </summary>
        /// <param name="buffer">The video buffer to send.</param>
        private void SendScreen(VideoMediaBuffer buffer)
        {
            // Send the video to our outgoing screen sharing video stream
            try
            {
                this.vbssSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"[OnVideoMediaReceived] Exception while calling vbssSocket.Send()");
            }
        }
        #endregion
    }
}
