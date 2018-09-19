// <copyright file="JoinCallRequestData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Data
{
    /// <summary>
    /// The join call body.
    /// </summary>
    public class JoinCallRequestData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallRequestData"/> class.
        /// </summary>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="meetingInfo">The meeting information.</param>
        public JoinCallRequestData(string tenantId, MeetingInfo meetingInfo)
        {
            this.TenantId = tenantId;
            this.MeetingInfo = meetingInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinCallRequestData"/> class.
        /// </summary>
        private JoinCallRequestData()
        {
        }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the meeting info.
        /// </summary>
        public MeetingInfo MeetingInfo { get; set; }
    }
}
