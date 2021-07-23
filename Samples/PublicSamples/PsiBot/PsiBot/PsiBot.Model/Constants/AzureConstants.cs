// <copyright file="AzureConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace PsiBot.Model.Constants
{
    /// <summary>
    /// Class AzureConstants.
    /// </summary>
    public static class AzureConstants
    {
        // Currently the service does not sign outbound request using AAD, instead it is signed
        // with a private certificate.  In order for us to be able to ensure the certificate is
        // valid we need to download the corresponding public keys from a trusted source.
        /// <summary>
        /// The authentication domain
        /// </summary>
        public const string AuthDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
    }
}
