// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorrelationId.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the CorrelationId type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.Common.Logging
{
    using System;
    using System.Runtime.Remoting.Messaging;

    /// <summary>
    /// The correlation id.
    /// </summary>
    public class CorrelationId
    {
        /// <summary>
        /// The logical data name.
        /// </summary>
        private const string LogicalDataName = "Sample.Common.Logging.CorrelationId";

        /// <summary>
        /// Sets the current correlation ID.  This is necessary to call in event handler callbacks because the event producer
        /// may not be aware of the call id.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetCurrentId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Holder holder = CallContext.LogicalGetData(LogicalDataName) as Holder;
            if (holder == null)
            {
                CallContext.LogicalSetData(LogicalDataName, new Holder { Id = value });
            }
            else
            {
                try
                {
                    holder.Id = value;
                }
                catch (AppDomainUnloadedException)
                {
                    CallContext.LogicalSetData(LogicalDataName, new Holder { Id = value });
                }
            }
        }

        /// <summary>
        /// Gets the current correlation id.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetCurrentId()
        {
            Holder holder = CallContext.LogicalGetData(LogicalDataName) as Holder;
            if (holder != null)
            {
                try
                {
                    return holder.Id;
                }
                catch (AppDomainUnloadedException)
                {
                    CallContext.FreeNamedDataSlot(LogicalDataName);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// The holder.
        /// </summary>
        private class Holder : MarshalByRefObject
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            public string Id { get; set; }
        }
    }
}