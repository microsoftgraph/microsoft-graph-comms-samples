// ***********************************************************************
// Assembly         : RecordingBot.Model
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="JoinURLResponse.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;

namespace RecordingBot.Model.Models
{
    /// <summary>
    /// Class JoinURLResponse.
    /// </summary>
    public partial class JoinURLResponse
    {
        /// <summary>
        /// Gets or sets the call identifier.
        /// </summary>
        /// <value>The call identifier.</value>
        [JsonProperty("callId")]
        public object CallId { get; set; }

        /// <summary>
        /// Gets or sets the scenario identifier.
        /// </summary>
        /// <value>The scenario identifier.</value>
        [JsonProperty("scenarioId")]
        public Guid ScenarioId { get; set; }

        /// <summary>
        /// Gets or sets the call.
        /// </summary>
        /// <value>The call.</value>
        [JsonProperty("call")]
        public string Call { get; set; }
    }
}
