using Azure;
using Azure.Messaging.EventGrid;
using RecordingBot.Model.Constants;
using RecordingBot.Model.Models;
using RecordingBot.Services.Contract;
using RecordingBot.Services.ServiceSetup;
using System;
using System.Collections.Generic;

namespace RecordingBot.Services.Util
{
    public class EventGridPublisher : IEventPublisher
    {
        string topicName = "recordingbotevents";
        string regionName = string.Empty;
        string topicKey = string.Empty;

        public EventGridPublisher(AzureSettings settings)
        {
            topicName = settings.TopicName;
            topicKey = settings.TopicKey;
            regionName = settings.RegionName;
        }

        public void Publish(string Subject, string Message, string TopicName)
        {
            if (TopicName.Length == 0)
                TopicName = topicName;

            var topicEndpoint = String.Format(BotConstants.topicEndpoint, TopicName, regionName); 

            if (topicKey?.Length > 0)
            { 
                var client = new EventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(topicKey));

                // Add event to list
                var eventsList = new List<EventGridEvent>();
                ListAddEvent(eventsList, Subject, Message);

                // Publish
                client.SendEventsAsync(eventsList).GetAwaiter().GetResult();
                if (Subject.StartsWith("CallTerminated"))
                    Console.WriteLine($"Publish to {TopicName} subject {Subject} message {Message}");
                else
                    Console.WriteLine($"Publish to {TopicName} subject {Subject}");
            }
            else
                Console.WriteLine($"Skipped publishing {Subject} events to Event Grid topic {TopicName} - No topic key specified");
        }

        static void ListAddEvent(List<EventGridEvent> eventsList, string Subject, string Message, string DataVersion = "2.0")
        {
            var eventGrid = new EventGridEvent(Subject, "RecordingBot.BotEventData", DataVersion, new BotEventData() { Message = Message })
            {
                EventTime = DateTime.Now
            };

            eventsList.Add(eventGrid);
        }
    }
}
