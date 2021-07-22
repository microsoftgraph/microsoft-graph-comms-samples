// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using PsiImage = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Represents a participant engagement component base class.
    /// </summary>
    public abstract class ParticipantEngagementBotBase : Subpipeline, ITeamsBot
    {
        /// <summary>
        /// Acoustic log energy threshold used for voice activity detection.
        /// </summary>
        protected const float EnergyThreshold = 6.0f;

        /// <summary>
        /// Video thumbnail scale relative to window size.
        /// </summary>
        protected const double ThumbnailWindowScale = 0.25;

        /// <summary>
        /// Video frame margin in pixels.
        /// </summary>
        protected const double FrameMarginWindowScale = 0.03;

        /// <summary>
        /// Video image border thickness in pixels.
        /// </summary>
        protected const int ImageBorderThickness = 4;

        private readonly Connector<Dictionary<string, (AudioBuffer, DateTime)>> audioInConnector;
        private readonly Connector<Dictionary<string, (Shared<PsiImage>, DateTime)>> videoInConnector;
        private readonly Connector<Shared<PsiImage>> screenShareOutConnector;

        private readonly TimeSpan speechWindow = TimeSpan.FromSeconds(5);
        private readonly Bitmap icon;
        private readonly Color backgroundColor = Color.FromArgb(71, 71, 71);
        private readonly Brush textBrush = Brushes.Black;
        private readonly Brush emptyThumbnailBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        private readonly Brush labelBrush = Brushes.Gray;
        private readonly Font statusFont = new (FontFamily.GenericSansSerif, 12);
        private readonly Font labelFont = new (FontFamily.GenericSansSerif, 36);

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantEngagementBotBase"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the rendered visual.</param>
        /// <param name="screenWidth">Width at which to render the shared screen.</param>
        /// <param name="screenHeight">Height at which to render the shared screen.</param>
        public ParticipantEngagementBotBase(Pipeline pipeline, TimeSpan interval, int screenWidth, int screenHeight)
            : base(pipeline, "ParticipantEngagementBot")
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            this.ScreenWidth = screenWidth;
            this.ScreenHeight = screenHeight;
            this.icon = new Bitmap("./icon.png");
            this.FrameMargin = (int)(Math.Max(screenWidth, screenHeight) * FrameMarginWindowScale);

            this.audioInConnector = this.CreateInputConnectorFrom<Dictionary<string, (AudioBuffer, DateTime)>>(pipeline, nameof(this.audioInConnector));
            this.videoInConnector = this.CreateInputConnectorFrom<Dictionary<string, (Shared<PsiImage>, DateTime)>>(pipeline, nameof(this.videoInConnector));
            this.screenShareOutConnector = this.CreateOutputConnectorTo<Shared<PsiImage>>(pipeline, nameof(this.screenShareOutConnector));

            // Compute some simple voice activity detection over each participant's audio stream,
            // then aggregate over a window to get a list of timestamps within the window that each
            // person was detected to have been speaking.
            var speech = this.audioInConnector.Parallel(
                (participantId, stream) =>
                {
                    var audioStream = stream.Select(tuple => tuple.Item1);
                    var acousticFeatures = audioStream.PipeTo(new AcousticFeaturesExtractor(audioStream.Out.Pipeline));

                    // Compute voice activity from the log-energy.
                    // The logic is very similar to what is described in this tutorial sample:
                    // https://github.com/Microsoft/psi-samples/tree/main/Samples/SimpleVoiceActivityDetector
                    var voiceActivity = acousticFeatures.LogEnergy
                        .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(300)))
                        .Aggregate(
                            false,
                            (previous, values) =>
                            {
                                var startedSpeech = !previous && values.All(v => v > EnergyThreshold);
                                var continuedSpeech = previous && !values.All(v => v < EnergyThreshold);
                                return startedSpeech || continuedSpeech;
                            });

                    // Internally, Parallel does an exact Join to produce its output stream. However, the
                    // cadence of messages coming out of the AcousticFeaturesExtractor differs from
                    // that of the incoming audio stream. This causes the Join to fail to produce
                    // messages. A workaround is the below relaxed Join to sync the cadence of the
                    // voice activity stream with the incoming audio stream and Select the voice activity values.
                    return stream.Join(voiceActivity, RelativeTimeInterval.Infinite).Select(x => x.Item3);
                }, name: "VoiceActivityDetection")
                .Select((m, e) => (VoiceActivity: m, e.OriginatingTime)) // lift time
                .Aggregate(
                    new Dictionary<string, List<DateTime>>(),
                    (aggregate, speaking) =>
                    {
                        // single current speaking time stamp (or empty sequence)
                        var current = speaking.VoiceActivity.Where(x => x.Value).Select(x => (x.Key, Times: Enumerable.Repeat(speaking.OriginatingTime, 1)));

                        // recent speech time stamps within speech window
                        var recent = aggregate.Select(x => (x.Key, Times: x.Value.Where(t => speaking.OriginatingTime - t < this.speechWindow)));

                        // aggregate dictionary of participant ID -> combined speech time stamps
                        return recent.Concat(current).Aggregate(
                            new Dictionary<string, List<DateTime>>(),
                            (dict, x) =>
                            {
                                if (dict.TryGetValue(x.Key, out var times))
                                {
                                    times.AddRange(x.Times);
                                }
                                else
                                {
                                    dict.Add(x.Key, x.Times.ToList());
                                }

                                return dict;
                            });
                    });

            // Aggregate participant video frames
            var video = this.videoInConnector.Aggregate(
                new Dictionary<string, Shared<PsiImage>>(),
                (aggregate, frames) =>
                {
                    // aggregate dictionary of participant ID -> video frame
                    foreach (var frame in frames)
                    {
                        if (aggregate.TryGetValue(frame.Key, out Shared<PsiImage> old))
                        {
                            old.Dispose();
                            aggregate.Remove(frame.Key);
                        }

                        aggregate.Add(frame.Key, frame.Value.Item1.AddRef());
                    }

                    return aggregate;
                });

            // Generate screen share frames on a regular clock.
            //
            // Note that a best effort is made to sync the video and speech streams with the
            // interval using relaxed Joins.
            Generators
                .Repeat(this, true, interval)
                .Join(speech, RelativeTimeInterval.Infinite)
                .Join(video, RelativeTimeInterval.Infinite, secondaryDeliveryPolicy: DeliveryPolicy.LatestMessage)
                .Process<(bool, Dictionary<string, List<DateTime>>, Dictionary<string, Shared<PsiImage>>), Shared<PsiImage>>(
                    (tuple, envelope, emitter) =>
                    {
                        this.ProduceScreenShare(tuple.Item3, tuple.Item2, envelope.OriginatingTime, emitter);
                    },
                    DeliveryPolicy.LatestMessage)
                .PipeTo(this.screenShareOutConnector, DeliveryPolicy.LatestMessage);
        }

        /// <inheritdoc/>
        public Receiver<Dictionary<string, (Shared<PsiImage>, DateTime)>> VideoIn => this.videoInConnector.In;

        /// <inheritdoc/>
        public Receiver<Dictionary<string, (AudioBuffer, DateTime)>> AudioIn => this.audioInConnector.In;

        /// <inheritdoc />
        public bool EnableScreenSharing => true;

        /// <inheritdoc />
        public (int Width, int Height) ScreenShareSize => (this.ScreenWidth, this.ScreenHeight);

        /// <inheritdoc />
        public Emitter<Shared<PsiImage>> ScreenShareOut => this.screenShareOutConnector.Out;

        /// <inheritdoc />
        public bool EnableVideoOutput => false;

        /// <inheritdoc />
        public (int Width, int Height) VideoSize => (this.ScreenWidth, this.ScreenHeight);

        /// <inheritdoc />
        public Emitter<Shared<PsiImage>> VideoOut { get; } = null;

        /// <inheritdoc />
        public bool EnableAudioOutput => false;

        /// <inheritdoc />
        public Emitter<AudioBuffer> AudioOut { get; } = null;

        /// <summary>
        /// Gets hilight color used for video frames and other colored elements.
        /// </summary>
        protected Color HighlightColor { get; private set; } = Color.FromArgb(69, 47, 156);

        /// <summary>
        /// Gets pixel width of the output screen.
        /// </summary>
        protected int ScreenWidth { get; private set; }

        /// <summary>
        /// Gets pixel height of the output screen.
        /// </summary>
        protected int ScreenHeight { get; private set; }

        /// <summary>
        /// Gets margin within which to render video frame.
        /// </summary>
        protected int FrameMargin { get; private set; }

        /// <summary>
        /// Render participant video frame.
        /// </summary>
        /// <param name="image">Video image.</param>
        /// <param name="pen">Pen with wich to draw image frame.</param>
        /// <param name="src">Source rectangle.</param>
        /// <param name="dest">Destination rectangle.</param>
        /// <param name="label">Label text.</param>
        /// <param name="graphics">Graphics context into which to render.</param>
        protected void RenderVideoFrame(PsiImage image, Pen pen, Rectangle src, Rectangle dest, string label, Graphics graphics)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            if (image != null)
            {
                graphics.DrawImage(image.ToBitmap(false), dest, src, GraphicsUnit.Pixel);
            }
            else
            {
                graphics.FillRectangle(this.emptyThumbnailBrush, dest.X, dest.Y, dest.Width, dest.Height);
                var size = graphics.MeasureString(label, this.labelFont);
                graphics.DrawString(label, this.labelFont, this.labelBrush, new PointF(dest.X + (dest.Width / 2) - (size.Width / 2), dest.Y + (dest.Height / 2) - (size.Height / 2)));
            }

            for (var i = 0; i < ImageBorderThickness - 1; i++)
            {
                var rx = dest.X - i;
                var ry = dest.Y - i;
                var rw = dest.Width + (2 * i);
                var rh = dest.Height + (2 * i);
                graphics.DrawRectangle(Pens.Black, rx, ry, rw, rh);
                graphics.DrawRectangle(pen, rx, ry, rw, rh);
            }
        }

        /// <summary>
        /// Update internal model.
        /// </summary>
        /// <param name="video">Current participant video frames.</param>
        /// <param name="speech">Current participant speech activitiy.</param>
        /// <param name="currentTime">Current pipeline time.</param>
        /// <returns>Current meeting participants.</returns>
        protected abstract IEnumerable<Participant> UpdateModel(Dictionary<string, Shared<PsiImage>> video, Dictionary<string, List<DateTime>> speech, DateTime currentTime);

        /// <summary>
        /// Generate a frame of shared screen to be emitted by the bot.
        /// </summary>
        /// <param name="participants">Current meeting participants.</param>
        /// <param name="graphics">Graphics context into which to render.</param>
        protected abstract void UpdateVisual(IEnumerable<Participant> participants, Graphics graphics);

        /// <summary>
        /// Create screen share video frame.
        /// </summary>
        /// <param name="video">Current participant video frames.</param>
        /// <param name="speech">Current participant speech activity.</param>
        /// <param name="originatingTime">Current originating time.</param>
        /// <param name="emitter">The emitter to post to.</param>
        private void ProduceScreenShare(Dictionary<string, Shared<PsiImage>> video, Dictionary<string, List<DateTime>> speech, DateTime originatingTime, Emitter<Shared<PsiImage>> emitter)
        {
            var participants = this.UpdateModel(video, speech, originatingTime);

            using var sharedImage = ImagePool.GetOrCreate(this.ScreenWidth, this.ScreenHeight, PixelFormat.BGRA_32bpp);
            var bitmap = new Bitmap(this.ScreenWidth, this.ScreenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.Clear(this.backgroundColor);
            graphics.DrawImage(this.icon, 10, this.ScreenHeight - (this.icon.Height / 2) - 10,  this.icon.Width / 2, this.icon.Height / 2);
            graphics.DrawString($"PsiBot - Powered by \\psi          {DateTime.Now:HH:mm:ss.FFF}", this.statusFont, this.textBrush, new PointF(48, this.ScreenHeight - 32));
            graphics.DrawString("MEETING IS BEING RECORDED", this.statusFont, Brushes.DarkRed, new PointF(48, 48));
            this.UpdateVisual(participants, graphics);
            sharedImage.Resource.CopyFrom(bitmap);
            graphics.Dispose();
            bitmap.Dispose();
            emitter?.Post(sharedImage, originatingTime);
        }

        /// <summary>
        /// Represents a meeting participant.
        /// </summary>
        protected class Participant
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Participant"/> class.
            /// </summary>
            /// <param name="thumbnail">Video thumbnail.</param>
            /// <param name="x">Horizontal position of video thumbnail as vector from center.</param>
            /// <param name="y">Vertical position of video thumbnail as vector from center.</param>
            /// <param name="width">Width of video thumbnail as unit screen width.</param>
            /// <param name="height">Height of video thumbnail as unit screen height.</param>
            /// <param name="label">Label text.</param>
            public Participant(Shared<PsiImage> thumbnail, double x, double y, double width, double height, string label = default)
            {
                this.Thumbnail = thumbnail;
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
                this.Label = label ?? string.Empty;
            }

            /// <summary>
            /// Gets horizontal position of video thumbnail as vector from center.
            /// </summary>
            public double X { get; }

            /// <summary>
            /// Gets vertical position of video thumbnail as vector from center.
            /// </summary>
            public double Y { get; }

            /// <summary>
            /// Gets label text.
            /// </summary>
            public string Label { get; }

            /// <summary>
            /// Gets latest video thumbnail.
            /// </summary>
            public Shared<PsiImage> Thumbnail { get; }

            /// <summary>
            /// Gets or sets width of video thumbnail as unit screen width.
            /// </summary>
            public double Width { get; set; }

            /// <summary>
            /// Gets or sets height of video thumbnail as unit screen height.
            /// </summary>
            public double Height { get; set; }

            /// <summary>
            /// Gets or sets recent (voice) activity level.
            /// </summary>
            public double Activity { get; set; }
        }
    }
}
