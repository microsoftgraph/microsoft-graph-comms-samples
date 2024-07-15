// <copyright file="GraphParticipantExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

#pragma warning disable SA1503
#pragma warning disable SA1611
#pragma warning disable SA1615
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Sample.PolicyRecordingBot.FrontEnd.Bot
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph.Contracts;
    using Microsoft.Graph.Models;

    public static class GraphParticipantExtensions
    {
        /// <summary>
        /// Same as <see cref="IdentitySetExtensions.GetPrimaryIdentityWithType"/> but a bit more efficient
        /// as it avoids invoking the methods or properties twice, once for null checking and then for returning.
        /// The priority order has been tweaked to move applicationInstance and application up.
        /// </summary>
        public static KeyValuePair<string, Identity> GetPrimaryIdentityWithType_NICE(this IdentitySet identitySet)
        {
            if (identitySet == null) return new KeyValuePair<string, Identity>(null, null);
            var user = identitySet.User;
            if (user != null) return new KeyValuePair<string, Identity>("user", user);
            var applicationInstance = identitySet.GetApplicationInstance();
            if (applicationInstance != null) return new KeyValuePair<string, Identity>("applicationInstance", applicationInstance);
            var guest = identitySet.GetGuest();
            if (guest != null) return new KeyValuePair<string, Identity>("guest", guest);
            var phone = identitySet.GetPhone();
            if (phone != null) return new KeyValuePair<string, Identity>("phone", phone);
            var application = identitySet.Application;
            if (application != null) return new KeyValuePair<string, Identity>("application", application);
            var encrypted = identitySet.GetEncrypted();
            if (encrypted != null) return new KeyValuePair<string, Identity>("encrypted", encrypted);
            var onPremises = identitySet.GetOnPremises();
            return onPremises != null ? new KeyValuePair<string, Identity>("onPremises", onPremises) : identitySet.GetEnumerator().FirstOrDefault();
        }
    }
}
