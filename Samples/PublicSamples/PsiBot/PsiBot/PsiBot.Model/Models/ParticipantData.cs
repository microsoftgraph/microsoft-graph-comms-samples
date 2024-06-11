// <copyright file="ParticipantData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Microsoft.Graph.Communications.Calls;
using System.Collections.Generic;

namespace PsiBot.Model.Models
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
