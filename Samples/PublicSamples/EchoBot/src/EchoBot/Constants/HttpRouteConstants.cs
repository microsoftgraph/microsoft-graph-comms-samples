namespace EchoBot.Constants
{
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
        /// The route for join call.
        /// </summary>
        public const string JoinCall = "joinCall";

        /// <summary>
        /// The route for getting the call.
        /// </summary>
        public const string CallRoute = Calls + "/{callLegId}";

        /// <summary>
        /// Route for incoming call requests.
        /// </summary>
        public const string HealthRoute = "health";
    }
}

