// <copyright file="HttpRouteConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Controllers
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api";

        /// <summary>
        /// Route for incoming requests including notifications, callbacks and incoming call.
        /// </summary>
        public const string OnIncomingRequestRoute = "calls";

        /// <summary>
        /// Route for join call request.
        /// </summary>
        public const string OnJoinCallRoute = "joinCall";

        /// <summary>
        /// Route for making outgoing call request.
        /// </summary>
        public const string OnMakeCallRoute = "makeCall";

        /// <summary>
        /// Route for Transfer call request.
        /// </summary>
        public const string OnTransferCallRoute = CallRoute + "transfer";

        /// <summary>
        /// Route for changing the answer call media type (local media or remote media).
        /// </summary>
        public const string AnswerWith = "answerWith";

        /// <summary>
        /// The calls suffix.
        /// </summary>
        public const string Calls = "calls/";

        /// <summary>
        /// The Logs suffix.
        /// </summary>
        public const string Logs = "logs/";

        /// <summary>
        /// Call Leg ID suffix.
        /// </summary>
        public const string CallIdTemplate = "{callId}/";

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string CallRoute = Calls + CallIdTemplate;

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string OnSnapshotRoute = CallRoute + "scr";

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string OnHueRoute = CallRoute + "hue";
    }
}
