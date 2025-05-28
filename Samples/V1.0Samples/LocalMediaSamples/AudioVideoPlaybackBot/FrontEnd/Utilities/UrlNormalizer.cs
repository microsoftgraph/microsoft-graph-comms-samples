// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlNormalizer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Utility class for normalizing URLs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.AudioVideoPlaybackBot.FrontEnd.UrlUtilities
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Utility class for normalizing URLs.
    /// </summary>
    public static class UrlNormalizer
    {
        /// <summary>
        /// Normalizes a Teams meeting URL to a format that can be parsed by the bot.
        /// </summary>
        /// <param name="joinUrl">The original Teams meeting URL.</param>
        /// <returns>A normalized URL that can be parsed by the bot.</returns>
        public static string NormalizeTeamsMeetingUrl(string joinUrl)
        {
            if (string.IsNullOrEmpty(joinUrl))
            {
                return joinUrl;
            }

            // Handle teams.live.com format
            if (joinUrl.Contains("teams.live.com/meet/"))
            {
                try
                {
                    // Extract the meeting ID and passcode
                    Uri uri = new Uri(joinUrl);
                    string path = uri.AbsolutePath;
                    string meetingId = path.Substring(path.LastIndexOf('/') + 1);

                    // Extract the passcode from query parameters if present
                    string passcode = string.Empty;
                    string query = uri.Query;
                    if (!string.IsNullOrEmpty(query) && query.Contains("p="))
                    {
                        passcode = query.Substring(query.IndexOf("p=") + 2);
                        if (passcode.Contains("&"))
                        {
                            passcode = passcode.Substring(0, passcode.IndexOf("&"));
                        }
                    }

                    // Construct a URL in the format that the bot can parse
                    return $"https://teams.microsoft.com/l/meetup-join/meeting_{meetingId}/0?context=%7b%22Tid%22%3a%22{Guid.NewGuid()}%22%2c%22Oid%22%3a%22{Guid.NewGuid()}%22%7d";
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("AudioVideoPlaybackService", $"Error normalizing Teams URL: {ex.Message}", EventLogEntryType.Error);
                    return joinUrl;
                }
            }

            return joinUrl;
        }
    }
}