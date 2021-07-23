// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using PsiImage = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Represents a participant engagement component base class.
    /// </summary>
    public class ParticipantEngagementScaleBot : ParticipantEngagementBotBase
    {
        private readonly bool varyHeights;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantEngagementScaleBot"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval at which to render and emit frames of the rendered visual.</param>
        /// <param name="screenWidth">Width at which to render the shared screen.</param>
        /// <param name="screenHeight">Height at which to render the shared screen.</param>
        /// <param name="varyHeights">Whether to vary the heights of participant video frames.</param>
        public ParticipantEngagementScaleBot(Pipeline pipeline, TimeSpan interval, int screenWidth, int screenHeight, bool varyHeights)
            : base(pipeline, interval, screenWidth, screenHeight)
        {
            this.varyHeights = varyHeights;
        }

        /// <inheritdoc />
        protected override IEnumerable<Participant> UpdateModel(Dictionary<string, Shared<PsiImage>> video, Dictionary<string, List<DateTime>> speech, DateTime originatingTime)
        {
            if (video == null)
            {
                throw new ArgumentNullException(nameof(video));
            }

            if (speech == null)
            {
                throw new ArgumentNullException(nameof(speech));
            }

            var num = video.Count;
            var w = 2.0 / num;
            var participants = new Dictionary<string, Participant>();
            var i = 0;
            foreach (var frame in video)
            {
                participants.Add(frame.Key, new Participant(frame.Value, (w * i++) + (w / 2) - 1.0, 0.0, 1.0 / num, 1.0 / num));
            }

            var overallTotalSpoken = speech.Sum(x => x.Value.Count);
            if (overallTotalSpoken > 0)
            {
                foreach (var s in speech)
                {
                    if (participants.TryGetValue(s.Key, out Participant p))
                    {
                        // adjust size to reflect activity as proportion of total speaking within speechWindow (sum of participants is 1.0)
                        p.Activity = Math.Max(0.0, Math.Min(1.0, (double)s.Value.Count / overallTotalSpoken));
                        var scale = 0.5 * (p.Activity - (1.0 / num));
                        p.Width = ((p.Width * 0.5) + (p.Width * scale)) * 2.0;
                        p.Height = ((p.Height * 0.5) + (p.Height * scale)) * 2.0;
                    }
                }
            }

            return participants.Values;
        }

        /// <inheritdoc />
        protected override void UpdateVisual(IEnumerable<ParticipantEngagementBotBase.Participant> participants, Graphics graphics)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            var num = participants.Count();
            if (num > 0)
            {
                var innerWidth = this.ScreenWidth - (this.FrameMargin * 2);
                var innerHeight = this.ScreenHeight - (this.FrameMargin * 2);
                var x = this.FrameMargin;
                foreach (var participant in participants)
                {
                    // assumes landscape
                    var h =
                        this.varyHeights ?
                        (int)(participant.Height * innerHeight) - ImageBorderThickness :
                        (int)(innerHeight / num * 2.0);
                    var y = (int)((participant.Y * (innerHeight / 2.0)) + (innerHeight / 2.0) - (h / 2)) + this.FrameMargin;
                    var w = (int)(participant.Width * innerWidth) - ImageBorderThickness;
                    var dest = new Rectangle(x, y, w, h);

                    using (var pen = new Pen(Color.FromArgb((int)(participant.Activity * 255.0), this.HighlightColor)))
                    {
                        var image = participant.Thumbnail?.Resource;
                        if (image != null)
                        {
                            var crop = (int)(participant.Width * image.Width);
                            var src =
                                this.varyHeights ?
                                new Rectangle(0, 0, image.Width, image.Height) :
                                new Rectangle((image.Width - crop) / 2, 0, crop, image.Height);
                            this.RenderVideoFrame(image, pen, src, dest, participant.Label, graphics);
                            x += w + (2 * ImageBorderThickness);
                        }
                        else
                        {
                            this.RenderVideoFrame(null, pen, Rectangle.Empty, dest, participant.Label, graphics);
                        }
                    }
                }
            }
        }
    }
}
