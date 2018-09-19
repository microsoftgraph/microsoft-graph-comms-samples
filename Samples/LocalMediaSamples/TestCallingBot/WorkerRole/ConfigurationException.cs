// <copyright file="ConfigurationException.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
namespace Sample.TestCallingBot.WorkerRole
{
    using System;

    /// <summary>
    /// Exception thrown when the configuration is not correct.
    /// </summary>
    internal sealed class ConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
        /// </summary>
        /// <param name="parameter">Parameters.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Other arguments.</param>
        internal ConfigurationException(string parameter, string message, params object[] args)
            : base(string.Format(message, args))
        {
            this.Parameter = parameter;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public string Parameter { get; private set; }

        /// <summary>
        /// Gets the Message.
        /// </summary>
        public override string Message
        {
            get
            {
                return string.Format(
                    "Parameter name: {0}\r\n{1}",
                    this.Parameter,
                    base.Message);
            }
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>string.</returns>
        public override string ToString()
        {
            return string.Format("Parameter name: {0}\r\n{1}", this.Parameter, base.ToString());
        }
    }
}
