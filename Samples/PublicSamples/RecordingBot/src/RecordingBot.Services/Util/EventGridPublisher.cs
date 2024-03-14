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
        private string _topicName = "recordingbotevents";
        private string _regionName = string.Empty;
        private string _topicKey = string.Empty;

        public EventGridPublisher(AzureSettings settings)
        {
            _topicName = settings.TopicName;
            _topicKey = settings.TopicKey;
            _regionName = settings.RegionName;
        }

        public void Publish(string subject, string message, string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                topicName = _topicName;
            }

            var topicEndpoint = string.Format(BotConstants.TOPIC_ENDPOINT, topicName, _regionName);

            if (_topicKey?.Length > 0)
            {
                var client = new EventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(_topicKey));

                // Add event to list
                var eventsList = new List<EventGridEvent>();
                ListAddEvent(eventsList, subject, message);

                // Publish
                client.SendEventsAsync(eventsList).GetAwaiter().GetResult();

                if (subject.StartsWith("CallTerminated"))
                {
                    Console.WriteLine($"Publish to {topicName} subject {subject} message {message}");
                }
                else
                {
                    Console.WriteLine($"Publish to {topicName} subject {subject}");
                }
            }
            else
            {
                Console.WriteLine($"Skipped publishing {subject} events to Event Grid topic {topicName} - No topic key specified");
            }
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
