// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
namespace Sample.OnlineMeeting
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.Common.Authentication;

    /// <summary>
    /// Default program class.
    /// </summary>
    public class Program
    {
        // Common settings.
        private static string appSecret = "__placeholder__";
        private static string appId = "__placeholder__";
        private static string tenantId = "__placeholder__";

        // Needed for app token meetings.
        private static string vtcId = "__placeholder__";

        // Needed for user token meetings.
        private static string userName = "__placeholder__";
        private static string password = "__placeholder__";

        private static Uri graphUri = new Uri("https://graph.microsoft.com/beta/");

        /// <summary>
        /// Gets the online meeting asynchronous.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="videoTeleconferenceId">The meeting identifier.</param>
        /// <returns> The onlinemeeting details. </returns>
        public static async Task<Microsoft.Graph.OnlineMeeting> GetOnlineMeetingByVtcIdAsync(string tenantId, string videoTeleconferenceId)
        {
            var name = typeof(Program).Assembly.GetName().Name;
            var logger = new GraphLogger(name);
            var onlineMeeting = new AppOnlineMeeting(
                        new AuthenticationProvider(name, appId, appSecret, logger),
                        graphUri);

            var meetingDetails = await onlineMeeting.GetOnlineMeetingByVtcIdAsync(tenantId, videoTeleconferenceId, default(Guid)).ConfigureAwait(false);

            Console.WriteLine(meetingDetails.VideoTeleconferenceId);
            Console.WriteLine(meetingDetails.ChatInfo.ThreadId);

            return meetingDetails;
        }

        /// <summary>
        /// Creates the online meeting asynchronous.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="organizerId">The organizer identifier.</param>
        /// <returns> The newly created onlinemeeting. </returns>
        [Obsolete("This way of creating meeting is obsolete. Check CreateUserMeetingRequestAsync for creating meetings.")]
        public static async Task<Microsoft.Graph.OnlineMeeting> CreateOnlineMeetingAsync(string tenantId, string organizerId)
        {
            var name = typeof(Program).Assembly.GetName().Name;
            var logger = new GraphLogger(name);
            var onlineMeeting = new AppOnlineMeeting(
                        new AuthenticationProvider(name, appId, appSecret, logger),
                        graphUri);

            var meetingDetails = await onlineMeeting.CreateOnlineMeetingAsync(tenantId, organizerId, default(Guid)).ConfigureAwait(false);

            Console.WriteLine(meetingDetails.Id);
            Console.WriteLine(meetingDetails.ChatInfo.ThreadId);

            return meetingDetails;
        }

        /// <summary>
        /// Creates the online meeting asynchronous.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns> The newly created onlinemeeting. </returns>
        public static async Task<Microsoft.Graph.OnlineMeeting> CreateUserOnlineMeetingAsync(string tenantId)
        {
            var name = typeof(Program).Assembly.GetName().Name;
            var logger = new GraphLogger(name);

            var onlineMeeting = new UserOnlineMeeting(
                        new UserPasswordAuthenticationProvider(name, appId, appSecret, userName, password, logger),
                        graphUri);

            var meetingDetails = await onlineMeeting.CreateUserMeetingRequestAsync(tenantId, default(Guid)).ConfigureAwait(false);

            Console.WriteLine(meetingDetails.Id);
            Console.WriteLine(meetingDetails.ChatInfo.ThreadId);

            return meetingDetails;
        }

        /// <summary>
        /// The Main entry point.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                try
                {
                    var meetingDetails = await GetOnlineMeetingByVtcIdAsync(tenantId, vtcId).ConfigureAwait(false);

                    /*
                     * THIS WAY OF CREATING MEETING IS OBSOLETE. CHECK CreateUserOnlineMeetingAsync FOR CREATING MEETINGS.
                     *
                     * var createdMeetingDetails = await CreateOnlineMeetingAsync(tenantId, organizerID).ConfigureAwait(false);
                    */

                    var userTokenMeeting = await CreateUserOnlineMeetingAsync(tenantId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            Console.ReadKey();
        }
    }
}
