// <copyright file="IncidentRequestData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The incident request data.
    /// </summary>
    public class IncidentRequestData
    {
        /// <summary>
        /// Gets or sets the incident name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the incident time.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the user object ids.
        /// </summary>
        public IEnumerable<string> ObjectIds { get; set; }

        /// <summary>
        /// Gets or sets the meeting info.
        /// </summary>
        public MeetingInfo MeetingInfo { get; set; }
    }
}
