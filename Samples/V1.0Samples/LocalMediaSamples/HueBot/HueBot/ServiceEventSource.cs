// <copyright file="ServiceEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.Threading.Tasks;

    /// <summary>
    /// HueBot Events.
    /// Define an instance method for each event you want to record and apply an [Event] attribute to it.
    /// The method name is the name of the event.
    /// Pass any parameters you want to record with the event (only primitive integer types, DateTime, Guid &amp; string are allowed).
    /// Each event method implementation should check whether the event source is enabled, and if it is, call WriteEvent() method to raise the event.
    /// The number and types of arguments passed to every event method must exactly match what is passed to WriteEvent().
    /// Put [NonEvent] attribute on all methods that do not define an event.
    /// For more information see https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.aspx.
    /// </summary>
    [EventSource(Name = "Sample.HueBot")]
    internal sealed class ServiceEventSource : EventSource
    {
        /// <summary>
        /// Current event source.
        /// </summary>
        public static readonly ServiceEventSource Current = new ServiceEventSource();

        /// <summary>
        /// Initializes static members of the <see cref="ServiceEventSource"/> class.
        /// </summary>
        static ServiceEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceEventSource"/> class.
        /// Instance constructor is private to enforce singleton semantics.
        /// </summary>
        private ServiceEventSource()
        {
        }

        /// <summary>
        /// Event ids.
        /// </summary>
        private enum EventId
        {
            /// <summary>
            /// General message
            /// </summary>
            MessageEventId = 1,

            /// <summary>
            /// Service message
            /// </summary>
            ServiceMessageEventId = 2,

            /// <summary>
            /// Type registration
            /// </summary>
            ServiceTypeRegisteredEventId = 3,

            /// <summary>
            /// Service host intialization
            /// </summary>
            ServiceHostInitializationFailedEventId = 4,

            /// <summary>
            /// Start request
            /// </summary>
            ServiceRequestStartEventId = 5,

            /// <summary>
            /// Stop request
            /// </summary>
            ServiceRequestStopEventId = 6,
        }

        /// <summary>
        /// Emit event.
        /// </summary>
        /// <param name="message">Text for the event.</param>
        [Event((int)EventId.MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent((int)EventId.MessageEventId, message);
            }
        }

        /// <summary>
        /// Emit service message.
        /// </summary>
        /// <param name="serviceContext">Service context.</param>
        /// <param name="message">Text for the event.</param>
        /// <param name="args">Formatting args.</param>
        [NonEvent]
        public void ServiceMessage(ServiceContext serviceContext, string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                var finalMessage = string.Format(message, args);
                this.ServiceMessage(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    GetReplicaOrInstanceId(serviceContext),
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    finalMessage);
            }
        }

        /// <summary>
        /// Emit service registered event.
        /// </summary>
        /// <param name="hostProcessId">Process id.</param>
        /// <param name="serviceType">Service type.</param>
        [Event((int)EventId.ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            this.WriteEvent((int)EventId.ServiceTypeRegisteredEventId, hostProcessId, serviceType);
        }

        /// <summary>
        /// Emit Service initialization failure event.
        /// </summary>
        /// <param name="exception">Exception data.</param>
        [Event((int)EventId.ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
        public void ServiceHostInitializationFailed(string exception)
        {
            this.WriteEvent((int)EventId.ServiceHostInitializationFailedEventId, exception);
        }

        /// <summary>
        /// A pair of events sharing the same name prefix with a "Start"/"Stop" suffix implicitly marks boundaries of an event tracing activity.
        /// These activities can be automatically picked up by debugging and profiling tools, which can compute their execution time, child activities,
        /// and other statistics.
        /// </summary>
        /// <param name="requestTypeName">Request type.</param>
        [Event((int)EventId.ServiceRequestStartEventId, Level = EventLevel.Informational, Message = "Service request '{0}' started", Keywords = Keywords.Requests)]
        public void ServiceRequestStart(string requestTypeName)
        {
            this.WriteEvent((int)EventId.ServiceRequestStartEventId, requestTypeName);
        }

        /// <summary>
        /// Service stop message.
        /// </summary>
        /// <param name="requestTypeName">Request type.</param>
        /// <param name="exception">Exception data.</param>
        [Event((int)EventId.ServiceRequestStopEventId, Level = EventLevel.Informational, Message = "Service request '{0}' finished", Keywords = Keywords.Requests)]
        public void ServiceRequestStop(string requestTypeName, string exception = "")
        {
            this.WriteEvent((int)EventId.ServiceRequestStopEventId, requestTypeName, exception);
        }

        /// <summary>
        /// Get instance.
        /// </summary>
        /// <param name="context">Service context.</param>
        /// <returns>Stateful or stateless instance.</returns>
        private static long GetReplicaOrInstanceId(ServiceContext context)
        {
            if (context is StatelessServiceContext stateless)
            {
                return stateless.InstanceId;
            }

            if (context is StatefulServiceContext stateful)
            {
                return stateful.ReplicaId;
            }

            throw new NotSupportedException("Context type not supported.");
        }

        /// <summary>
        /// For very high-frequency events it might be advantageous to raise events using WriteEventCore API.
        /// This results in more efficient parameter handling, but requires explicit allocation of EventData structure and unsafe code.
        /// To enable this code path, define UNSAFE conditional compilation symbol and turn on unsafe code support in project properties.
        /// </summary>
        /// <param name="serviceName">Service name.</param>
        /// <param name="serviceTypeName">Service type.</param>
        /// <param name="replicaOrInstanceId">Replica or instance id.</param>
        /// <param name="partitionId">Partition id.</param>
        /// <param name="applicationName">Aplication name.</param>
        /// <param name="applicationTypeName">Application type.</param>
        /// <param name="nodeName">Node name.</param>
        /// <param name="message">Custom text.</param>
        [Event((int)EventId.ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
        private void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {
            this.WriteEvent((int)EventId.ServiceMessageEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, message);
        }

        /// <summary>
        /// Event keywords can be used to categorize events.
        /// Each keyword is a bit flag.A single event can be associated with multiple keywords(via EventAttribute.Keywords property).
        /// Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// Requests.
            /// </summary>
            public const EventKeywords Requests = (EventKeywords)0x1L;

            /// <summary>
            /// Service Initialization.
            /// </summary>
            public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
        }
    }
}
