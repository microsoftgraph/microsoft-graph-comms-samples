namespace RecordingBot.Model.Constants
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CALL_SIGNALING_ROUTE_PREFIX = "api/calling";

        /// <summary>
        /// Route for incoming call requests.
        /// </summary>
        public const string ON_INCOMING_REQUEST_ROUTE = "";

        /// <summary>
        /// Route for incoming notification requests.
        /// </summary>
        public const string ON_NOTIFICATION_REQUEST_ROUTE = "notification";

        /// <summary>
        /// The calls route for both GET and POST.
        /// </summary>
        public const string CALLS = "calls";

        /// <summary>
        /// The route for join call.
        /// </summary>
        public const string JOIN_CALLS = "joinCall";

        /// <summary>
        /// The route for getting the call.
        /// </summary>
        public const string CALL_ROUTE = CALLS + "/{callLegId}";
    }
}
