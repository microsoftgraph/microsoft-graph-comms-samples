// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using Microsoft.Psi.Audio;
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

        private readonly TimeSpan speechWindow = TimeSpan.FromSeconds(5);
        private readonly Bitmap icon;
        private readonly Color backgroundColor = Color.FromArgb(71, 71, 71);
        private readonly Brush textBrush = Brushes.Black;
        private readonly Brush emptyThumbnailBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        private readonly Brush labelBrush = Brushes.Gray;
        private readonly Font statusFont = new Font(FontFamily.GenericSansSerif, 12);
        private readonly Font labelFont = new Font(FontFamily.GenericSansSerif, 36);

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantEngagementBotBase"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the rendered visual.</param>
        /// <param name="screenWidth">Width at which to render the shared screen.</param>
        /// <param name="screenHeight">Height at which to render the shared screen.</param>
        public ParticipantEngagementBotBase(Pipeline pipeline, TimeSpan interval, int screenWidth, int screenHeight)
            : base(pipeline, nameof(ParticipantEngagementBotBase))
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            this.ScreenWidth = screenWidth;
            this.ScreenHeight = screenHeight;
            this.icon = new Bitmap("./icon.png");
            this.FrameMargin = (int)(Math.Max(screenWidth, screenHeight) * FrameMarginWindowScale);

            var audioConnector = pipeline.CreateConnector<Dictionary<string, AudioBuffer>>(nameof(this.AudioIn));
            var videoConnector = pipeline.CreateConnector<Dictionary<string, Shared<PsiImage>>>(nameof(this.VideoIn));
            this.AudioIn = audioConnector.In;
            this.VideoIn = videoConnector.In;
            this.AudioOut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.AudioOut));
            this.VideoOut = pipeline.CreateEmitter<Shared<PsiImage>>(this, nameof(this.VideoOut));
            this.ScreenShareOut = pipeline.CreateEmitter<Shared<PsiImage>>(this, nameof(this.ScreenShareOut));

            var speech = audioConnector.Out.Parallel<string, AudioBuffer, bool>(
                (id, stream) =>
                {
                    var acoustic = new AcousticFeaturesExtractor(stream.Out.Pipeline);
                    stream.PipeTo(acoustic);
                    var activity = acoustic.LogEnergy
                        .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(300)))
                        .Aggregate(
                            false,
                            (previous, values) =>
                            {
                                var startedSpeech = !previous && values.First() > EnergyThreshold;
                                var continuedSpeech = previous && !values.All(v => v < EnergyThreshold);
                                return startedSpeech || continuedSpeech;
                            });

                    // Join to ensure cadence aligns - Parallel internally does an exact join
                    //
                    // Internally, Parallel does a Join to produce its output stream. However, the
                    // cadence of messages coming out of the AcousticFeaturesExtractor differs from
                    // that of the incomming audio stream. This causes the Join to fail to produce
                    // messaged. A workaround is the below relaxed Join to sync the cadence of the
                    // `speech` stream with the incomming `stream` and Select the speech values.
                    return stream.Join(activity, RelativeTimeInterval.Infinite).Select(x => x.Item2);
                })
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

            var video = videoConnector.Out.Aggregate(
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

                        aggregate.Add(frame.Key, frame.Value.AddRef());
                    }

                    return aggregate;
                });

            // Generate screen share frames an an `interval`
            //
            // Note that a best effort is made to sync the video and speech streams with the
            // interval using relaxed Joins. The final Do passes these values to ProduceScreenShare
            // which posts the rendered screen. The reason this is a side-effecting call rather than
            // producing the stream directly (with a Select over the rendering function for example)
            // is that ProduceScreenShare needs to create Shared<Image> instances and manage their
            // lifetimes; disposing after posting.
            Generators
                .Repeat(pipeline, true, interval)
                .Join(video, RelativeTimeInterval.Infinite)
                .Join(speech, RelativeTimeInterval.Infinite)
                .Do((m, e) => this.ProduceScreenShare(m.Item2, m.Item3, e.OriginatingTime));
        }

        /// <summary>
        /// Gets the receiver of participant video input.
        /// </summary>
        public Receiver<Dictionary<string, Shared<PsiImage>>> VideoIn { get; private set; }

        /// <summary>
        /// Gets the receiver of participant audio input.
        /// </summary>
        public Receiver<Dictionary<string, AudioBuffer>> AudioIn { get; private set; }

        /// <inheritdoc />
        public bool EnableScreenSharing => true;

        /// <inheritdoc />
        public (int Width, int Height) ScreenShareSize => (this.ScreenWidth, this.ScreenHeight);

        /// <inheritdoc />
        public Emitter<Shared<PsiImage>> ScreenShareOut { get; private set; }

        /// <inheritdoc />
        public bool EnableVideoOutput => false;

        /// <inheritdoc />
        public (int Width, int Height) VideoSize => (this.ScreenWidth, this.ScreenHeight);

        /// <inheritdoc />
        public Emitter<Shared<PsiImage>> VideoOut { get; private set; }

        /// <inheritdoc />
        public bool EnableAudioOutput => false;

        /// <inheritdoc />
        public Emitter<AudioBuffer> AudioOut { get; private set; }

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
        protected void ProduceScreenShare(Dictionary<string, Shared<PsiImage>> video, Dictionary<string, List<DateTime>> speech, DateTime originatingTime)
        {
            var participants = this.UpdateModel(video, speech, originatingTime);

            using (var sharedImage = ImagePool.GetOrCreate(this.ScreenWidth, this.ScreenHeight, PixelFormat.BGRA_32bpp))
            {
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
                this.ScreenShareOut.Post(sharedImage, originatingTime);
            }
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
