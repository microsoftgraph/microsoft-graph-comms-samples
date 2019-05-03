// <copyright file="Extensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Bot
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Extensions for Bot.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Clamp value into range.
        /// </summary>
        /// <param name="val">Value to clamp.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>Clamped value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(this int val, int min, int max) =>
            val < min ? min :
            val > max ? max : val;
    }
}
