// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpRouteConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   HTTP route constants for routing requests to CallController methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.PolicyRecordingBot.FrontEnd.Http
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route for incoming call requests.
        /// </summary>
        public const string OnIncomingRequestRoute = "";

        /// <summary>
        /// Route for incoming notification requests.
        /// </summary>
        public const string OnNotificationRequestRoute = "notification";

        /// <summary>
        /// The logs route for GET.
        /// </summary>
        public const string Logs = "logs";

        /// <summary>
        /// The calls route for both GET and POST.
        /// </summary>
        public const string Calls = "calls";

        /// <summary>
        /// The route for getting the call.
        /// </summary>
        public const string CallRoute = Calls + "/{callLegId}";
    }
}