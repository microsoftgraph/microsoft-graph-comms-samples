// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The utilities class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd
{
    using Microsoft.Skype.Bots.Media;

    /// <summary>
    /// The utility class.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Helper function get id.
        /// </summary>
        /// <param name="videoFormat">Video format.</param>
        /// <returns>The <see cref="int"/> of the video format.</returns>
        public static int GetId(this VideoFormat videoFormat)
        {
            return $"{videoFormat.VideoColorFormat}{videoFormat.Width}{videoFormat.Height}".GetHashCode();
        }
    }
}
