// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a Teams bot component.
    /// </summary>
    public interface ITeamsBot
    {
        /// <summary>
        /// Gets the receiver of participant video input with timestamps.
        /// </summary>
        Receiver<Dictionary<string, (Shared<Image>, DateTime)>> VideoIn { get; }

        /// <summary>
        /// Gets the receiver of participant audio input with timestamps.
        /// </summary>
        Receiver<Dictionary<string, (AudioBuffer, DateTime)>> AudioIn { get; }

        /// <summary>
        /// Gets a value indicating whether to enable screen sharing.
        /// </summary>
        bool EnableScreenSharing { get; }

        /// <summary>
        /// Gets size of shared screen (1920×1080 1280×720 960×540 640×360 480×270 424×240 320×180).
        /// </summary>
        /// <remarks>See https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/bot_media/Microsoft.Skype.Bots.Media.VideoFormat.html
        /// for the list of video formats and sizes provided in Microsoft.Skype.Bots.Media.</remarks>
        (int Width, int Height) ScreenShareSize { get; }

        /// <summary>
        /// Gets the emitter that generates bot shared screen output.
        /// </summary>
        Emitter<Shared<Image>> ScreenShareOut { get; }

        /// <summary>
        /// Gets a value indicating whether to enable video output.
        /// </summary>
        bool EnableVideoOutput { get; }

        /// <summary>
        /// Gets size of video (1920×1080 1280×720 960×540 640×360 480×270 424×240 320×180).
        /// </summary>
        /// <remarks>See https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/bot_media/Microsoft.Skype.Bots.Media.VideoFormat.html
        /// for the list of video formats and sizes provided in Microsoft.Skype.Bots.Media.</remarks>
        (int Width, int Height) VideoSize { get; }

        /// <summary>
        /// Gets the emitter that generates bot video output.
        /// </summary>
        Emitter<Shared<Image>> VideoOut { get; }

        /// <summary>
        /// Gets a value indicating whether to enable audio output.
        /// </summary>
        bool EnableAudioOutput { get; }

        /// <summary>
        /// Gets the emitter that generates bot audio output.
        /// </summary>
        Emitter<AudioBuffer> AudioOut { get; }
    }
}
