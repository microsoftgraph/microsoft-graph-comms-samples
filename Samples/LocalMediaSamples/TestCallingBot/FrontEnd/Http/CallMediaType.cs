// <copyright file="CallMediaType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.TestCallingBot.FrontEnd.Http
{
    /// <summary>
    /// Media bot type (local or remote).
    /// </summary>
    public enum CallMediaType
    {
        /// <summary>
        /// Bot hosts the media processing
        /// </summary>
        Local,

        /// <summary>
        /// Media processed remotely
        /// </summary>
        Remote,
    }
}