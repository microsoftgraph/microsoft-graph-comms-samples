// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="ParticipantData.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Graph.Communications.Calls;
using System.Collections.Generic;

namespace RecordingBot.Model.Models
{
    /// <summary>
    /// Class ParticipantData.
    /// </summary>
    public class ParticipantData
    {
        /// <summary>
        /// Gets or sets the added resources.
        /// </summary>
        /// <value>The added resources.</value>
        public ICollection<IParticipant> AddedResources { get; set; }
        /// <summary>
        /// Gets or sets the removed resources.
        /// </summary>
        /// <value>The removed resources.</value>
        public ICollection<IParticipant> RemovedResources { get; set; }
    }
}
