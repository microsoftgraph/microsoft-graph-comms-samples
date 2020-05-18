// <copyright file="UserRequestData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.ReminderBot.Data
{
    using System;

    /// <summary>
    /// The user request data.
    /// </summary>
    public class UserRequestData
    {
        /// <summary>
        /// Gets or sets the user object id.
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; set; }
    }
}
