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
        /// Gets or sets the join URL.
        /// </summary>
        public string JoinURL { get; set; }

        /// <summary>
        /// Gets or sets the meeting identifier.
        /// </summary>
        public string MeetingId { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the correlation id.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to remove the bot from default routing group.
        /// </summary>
        public bool RemoveFromDefaultRoutingGroup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether allow conversation without host.
        /// </summary>
        public bool AllowConversationWithoutHost { get; set; }
    }
}
