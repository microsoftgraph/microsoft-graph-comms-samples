// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.OnlineMeeting
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Default program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The Main entry point.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            string meetingId = "__placeholder__";
            string appId = "__placeholder__";
            string appSecret = "__placeholder__?";
            string tenantId = "__placeholder__";

            Task.Run(async () =>
            {
                try
                {
                    var onlineMeeting = new OnlineMeeting(new RequestAuthenticationProvider(appId, appSecret));
                    var meetingDetails = await onlineMeeting.GetOnlineMeetingAsync(tenantId, meetingId).ConfigureAwait(false);

                    Console.WriteLine(meetingDetails.Id);
                    Console.WriteLine(meetingDetails.ChatInfo.ThreadId);
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
