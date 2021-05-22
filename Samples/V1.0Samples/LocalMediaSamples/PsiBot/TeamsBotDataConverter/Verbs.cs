// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using CommandLine;

    /// <summary>
    /// Command-line verbs.
    /// </summary>
    internal class Verbs
    {
        /// <summary>
        /// Base command-line options.
        /// </summary>
        internal abstract class BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets file path to Psi data store.
            /// </summary>
            [Option('p', "path", HelpText = "File path to Psi data store (default=working directory).")]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets name of Psi data store.
            /// </summary>
            [Option('d', "data", Required = true, HelpText = "Name of Psi data store(s).")]
            public string Store { get; set; }

            /// <summary>
            /// Gets or sets number of messages to include.
            /// </summary>
            [Option('q', "quality", Default = 90, HelpText = "Quality of JPEG compression 0-100 (optional, default 90).")]
            public int Quality { get; set; }
        }

        /// <summary>
        /// Encode image streams verb.
        /// </summary>
        [Verb("encode", HelpText = "Encode image streams to JPEG.")]
        internal class Encode : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of output Psi data store.
            /// </summary>
            [Option('o', "output", Required = false, Default = "Encoded", HelpText = "Name of output Psi data store (default=Encoded).")]
            public string Output { get; set; }
        }

        /// <summary>
        /// Split dictionary streams verb.
        /// </summary>
        [Verb("split", HelpText = "Split dictionary streams.")]
        internal class Split : BaseStoreCommand
        {
            /// <summary>
            /// Gets or sets name of output Psi data store.
            /// </summary>
            [Option('o', "output", Required = false, Default = "Split", HelpText = "Name of output Psi data store (default=Split).")]
            public string Output { get; set; }
        }
    }
}