// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpRouteConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   HTTP route constants for routing requests to CallController methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.TestCallingBot.FrontEnd.Http
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route for media prompts.
        /// </summary>
        public const string OnPromptsRoute = "prompts/";

        /// <summary>
        /// Call Leg ID suffix.
        /// </summary>
        public const string PromptUriTemplate = "{promptId}/";

        /// <summary>
        /// Route for a specific media prompt.
        /// </summary>
        public const string PromptRoute = OnPromptsRoute + PromptUriTemplate;

        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route for incoming requests including notifications, callbacks and incoming call.
        /// </summary>
        public const string OnIncomingRequestRoute = "call";

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
        /// Route for configureMixer call request.
        /// </summary>
        public const string OnConfigureMixerRoute = CallRoute + "configureMixer";

        /// <summary>
        /// Route for adding participants request.
        /// </summary>
        public const string OnAddParticipantsRoute = CallRoute + "addParticipants";

        /// <summary>
        /// Route for adding audio routing group request.
        /// </summary>
        public const string OnAddAudioRoutingGroupRoute = CallRoute + "addAudioRoutingGroup";

        /// <summary>
        /// Route for updating audio routing group request.
        /// </summary>
        public const string OnUpdateAudioRoutingGroupRoute = CallRoute + "updateAudioRoutingGroup";

        /// <summary>
        /// Route for deleting audio routing group request.
        /// </summary>
        public const string OnDeleteAudioRoutingGroupRoute = CallRoute + "deleteAudioRoutingGroup";

        /// <summary>
        /// Route for muting participants request.
        /// </summary>
        public const string OnMute = CallRoute + "mute";

        /// <summary>
        /// Route for unmuting request.
        /// </summary>
        public const string OnUnmute = CallRoute + "unmute";

        /// <summary>
        /// Route for changing the answer call medai type(local media or remote media).
        /// </summary>
        public const string AnswerWith = "answerWith";

        /// <summary>
        /// Route for subscribe to tone request.
        /// </summary>
        public const string OnSubscribeToToneRoute = CallRoute + "subscribeToTone";

        /// <summary>
        /// The calls suffix.
        /// </summary>
        public const string Calls = "calls/";

        /// <summary>
        /// Call Leg ID suffix.
        /// </summary>
        public const string CallLegIdUriTemplate = "{callLegId}/";

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string CallRoute = Calls + CallLegIdUriTemplate;

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string OnGetScreenshotRoute = CallRoute + "scr";

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string OnPutHueRoute = CallRoute + "hue";
    }
}