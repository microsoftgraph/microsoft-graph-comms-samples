// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-03-2020
// ***********************************************************************
// <copyright file="BotConstants.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace RecordingBot.Model.Constants
{
    /// <summary>
    /// Class BotConstants.
    /// </summary>
    public static class BotConstants
    {
        /// <summary>
        /// The number of multiview sockets
        /// </summary>
        public const uint NumberOfMultiviewSockets = 3;
        /// <summary>
        /// The default output folder
        /// </summary>
        public const string DefaultOutputFolder = "teams-recording-bot";
        // event string formatting required for topic endpoint -- topic name and region name
        // e.g. https://<YOUR-TOPIC-NAME>.<REGION-NAME>-1.eventgrid.azure.net/api/events";
        /// <summary>
        /// The topic endpoint
        /// </summary>
        public const string topicEndpoint = "https://{0}.{1}-1.eventgrid.azure.net/api/events";
    }
}
