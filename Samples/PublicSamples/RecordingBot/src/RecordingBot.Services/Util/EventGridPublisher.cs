// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="EventGridPublisher.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Models;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Collections.Generic;

namespace RecordingBot.Services.Util
{

    /// <summary>
    /// Class EventGridPublisher.
    /// Implements the <see cref="RecordingBot.Services.Contract.IEventPublisher" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Contract.IEventPublisher" />
    public class EventGridPublisher : IEventPublisher
    {
        /// <summary>
        /// The topic name
        /// </summary>
        string topicName = "recordingbotevents";
        /// <summary>
        /// The region name
        /// </summary>
        string regionName = string.Empty;
        /// <summary>
        /// The topic key
        /// </summary>
        string topicKey = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher" /> class.

        /// </summary>
        /// <param name="settings">The settings.</param>
        public EventGridPublisher(AzureSettings settings)
        {
            this.topicName = settings.TopicName;
            this.topicKey = settings.TopicKey;
            this.regionName = settings.RegionName;
        }

        /// <summary>
        /// Publishes the specified subject.
        /// </summary>
        /// <param name="Subject">The subject.</param>
        /// <param name="Message">The message.</param>
        /// <param name="TopicName">Name of the topic.</param>
        public void Publish(string Subject, string Message, string TopicName)
        {
            if (TopicName.Length == 0)
                TopicName = this.topicName;

            var topicEndpoint = String.Format(BotConstants.topicEndpoint, TopicName, this.regionName); 
            var topicKey = this.topicKey;

            if (topicKey?.Length > 0)
            { 
                var topicHostname = new Uri(topicEndpoint).Host;
                var topicCredentials = new TopicCredentials(topicKey);
                var client = new EventGridClient(topicCredentials);

                // Add event to list
                var eventsList = new List<EventGridEvent>();
                ListAddEvent(eventsList, Subject, Message);

                // Publish
                client.PublishEventsAsync(topicHostname, eventsList).GetAwaiter().GetResult();
                if (Subject.StartsWith("CallTerminated"))
                    Console.WriteLine($"Publish to {TopicName} subject {Subject} message {Message}");
                else
                    Console.WriteLine($"Publish to {TopicName} subject {Subject}");
            }
            else
                Console.WriteLine($"Skipped publishing {Subject} events to Event Grid topic {TopicName} - No topic key specified");
        }

        /// <summary>
        /// Lists the add event.
        /// </summary>
        /// <param name="eventsList">The events list.</param>
        /// <param name="Subject">The subject.</param>
        /// <param name="Message">The message.</param>
        /// <param name="DataVersion">The data version.</param>
        static void ListAddEvent(List<EventGridEvent> eventsList, string Subject, string Message, string DataVersion = "2.0")
        {
            eventsList.Add(new EventGridEvent()
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "RecordingBot.BotEventData",
                Data = new BotEventData()
                {
                    Message = Message
                },
                EventTime = DateTime.Now,
                Subject = Subject,
                DataVersion = DataVersion
            });
        }
    }
}
