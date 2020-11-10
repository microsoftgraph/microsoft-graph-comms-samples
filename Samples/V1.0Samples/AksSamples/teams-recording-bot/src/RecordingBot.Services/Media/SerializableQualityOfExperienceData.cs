// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="SerializableQualityOfExperienceData.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingBot.Services.Media
{
    /// <summary>
    /// Class SerializableAudioQualityOfExperienceData.
    /// </summary>
    public class SerializableAudioQualityOfExperienceData
    {
        /// <summary>
        /// The identifier
        /// </summary>
        public string Id;
        /// <summary>
        /// The average in bound network jitter
        /// </summary>
        public long AverageInBoundNetworkJitter;
        /// <summary>
        /// The maximum in bound network jitter
        /// </summary>
        public long MaximumInBoundNetworkJitter;
        /// <summary>
        /// The total media duration
        /// </summary>
        public long TotalMediaDuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableAudioQualityOfExperienceData" /> class.

        /// </summary>
        /// <param name="Id">The identifier.</param>
        /// <param name="aQoE">a qo e.</param>
        public SerializableAudioQualityOfExperienceData(string Id, AudioQualityOfExperienceData aQoE)
        {
            this.Id = Id;
            AverageInBoundNetworkJitter = aQoE.AudioMetrics.AverageInboundNetworkJitter.Ticks;
            MaximumInBoundNetworkJitter = aQoE.AudioMetrics.MaximumInboundNetworkJitter.Ticks;
            TotalMediaDuration = aQoE.TotalMediaDuration.Ticks;
        }
    }
}
