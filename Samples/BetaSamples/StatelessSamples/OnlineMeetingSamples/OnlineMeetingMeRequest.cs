// <copyright file="OnlineMeetingMeRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
//
#pragma warning disable SA1100  // Do not prefix calls with base
#pragma warning disable SA1402  // File may contain only single type.
#pragma warning disable SA1121  // Use built-int type alias.
#pragma warning disable SA1649  // Filename should match first type.

namespace Microsoft.Graph
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// IUserRequestBuilder with OnlineMeetings request collection property.
    /// </summary>
    public interface IUserRequestBuilderEx : IUserRequestBuilder
    {
        /// <summary>
        /// Gets.
        /// </summary>
        IUserOnlineMeetingsCollectionRequestBuilder OnlineMeetings
        {
            get;
        }
    }

    /// <summary>
    /// UserRequestBuilderEx with support for creating onlinemeetings request collection.
    /// </summary>
    public class UserRequestBuilderEx : UserRequestBuilder, IUserRequestBuilderEx
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRequestBuilderEx"/> class.
        /// </summary>
        /// <param name="requestUrl">The URL for the built request.</param>
        /// <param name="client">The Microsoft.Graph.IBaseClient for handling requests.</param>
        public UserRequestBuilderEx(String requestUrl, IBaseClient client)
            : base(requestUrl, client)
        {
        }

        /// <summary>
        /// Gets.
        /// </summary>
        public IUserOnlineMeetingsCollectionRequestBuilder OnlineMeetings => new UserOnlineMeetingsCollectionRequestBuilder(this.AppendSegmentToRequestUrl("onlinemeetings"), base.Client);
    }

    /// <summary>
    /// CallsGraphServiceClientEx adding suport for me with onlinemeetings request collection.
    /// </summary>
    public class CallsGraphServiceClientEx : CallsGraphServiceClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallsGraphServiceClientEx"/> class.
        /// </summary>
        /// <param name="baseUrl">a.</param>
        /// <param name="authenticationProvider">b.</param>
        /// <param name="httpProvider">c.</param>
        public CallsGraphServiceClientEx(string baseUrl, IAuthenticationProvider authenticationProvider, IHttpProvider httpProvider = null)
           : base(baseUrl, authenticationProvider, httpProvider)
        {
        }

        /// <summary>
        /// Gets.
        /// </summary>
        public new IUserRequestBuilderEx Me => new UserRequestBuilderEx(base.BaseUrl + "/me", this);
    }
}

#pragma warning restore SA1100  // Do not prefix calls with base
#pragma warning restore SA1402  // File may contain only single type.
#pragma warning restore SA1121  // Use built-int type alias.
#pragma warning restore SA1649  // Filename should match first type.