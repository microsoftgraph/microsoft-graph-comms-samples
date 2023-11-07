// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="ParticipantExtension.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Resources;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace RecordingBot.Model.Extension
{
    /// <summary>
    /// Class ParticipantExtension.
    /// Implements the <see cref="Microsoft.Graph.Communications.Calls.IParticipant" />
    /// </summary>
    /// <seealso cref="Microsoft.Graph.Communications.Calls.IParticipant" />
    public class ParticipantExtension : IParticipant
    {
        /// <summary>
        /// Gets the stateful participant resource.
        /// </summary>
        /// <value>The resource.</value>
        public Participant Resource { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantExtension"/> class.
        /// </summary>
        /// <param name="participant">The participant.</param>
        [JsonConstructor]
        public ParticipantExtension(IParticipant participant)
        {
            this.Resource = participant?.Resource;
            this.Id = participant?.Id;
            this.ResourcePath = participant?.ResourcePath;

            if (participant != null)
            {
                this.ModifiedDateTime = participant.ModifiedDateTime;
                participant.OnUpdated += this.OnUpdated;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantExtension"/> class.
        /// </summary>
        /// <param name="participant">The participant.</param>
        public ParticipantExtension(Participant participant)
        {
            this.Resource = participant;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the last modified date time of this resource.
        /// </summary>
        /// <value>The modified date time.</value>
        public DateTimeOffset ModifiedDateTime { get; private set; }

        /// <summary>
        /// Gets the created date time of this resource.
        /// </summary>
        /// <value>The created date time.</value>
        public DateTimeOffset CreatedDateTime { get; private set; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        [JsonIgnore]
        public ICommunicationsClient Client { get; private set; }

        /// <summary>
        /// Gets the graph client.
        /// </summary>
        /// <value>The graph client.</value>
        [JsonIgnore]
        public IGraphClient GraphClient { get; private set; }

        /// <summary>
        /// Gets the graph logger.
        /// </summary>
        /// <value>The graph logger.</value>
        [JsonIgnore]
        public IGraphLogger GraphLogger { get; private set; }

        /// <summary>
        /// Gets the resource path for this collection.
        /// </summary>
        /// <value>The resource path.</value>
        public string ResourcePath { get; private set; }

        /// <summary>
        /// Gets the stateful participant resource.
        /// </summary>
        /// <value>The resource.</value>
        /// <exception cref="NotImplementedException"></exception>
        [JsonIgnore]
        object IResource.Resource => throw new NotImplementedException();

        /// <summary>
        /// Event fired when this resource has been updated.
        /// </summary>
        public event ResourceEventHandler<IParticipant, Participant> OnUpdated;

        /// <summary>
        /// Deletes this participant asynchronously.
        /// </summary>
        /// <param name="handleHttpNotFoundInternally">If the <see cref="T:Microsoft.Graph.Communications.Calls.IParticipant" /> is already gone, whether to handle the exception gracefully or not.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> for the request.</param>
        /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that completes after the request has been sent.
        /// The completion of this task does not guarantee deletion. Confirmation of
        /// deletion comes as a notification and can be subscribed by IParticipant.OnUpdated and
        /// IParticipantCollection.OnUpdated</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task DeleteAsync(bool handleHttpNotFoundInternally = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs the mute operation asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that completes after the request has been sent.
        /// The mute notification will come in on IParticipant.OnUpdated</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task MuteAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
