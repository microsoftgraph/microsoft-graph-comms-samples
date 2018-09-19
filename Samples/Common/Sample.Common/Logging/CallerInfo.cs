// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CallerInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Class that encapsulates the caller's (creator's) information.  This is helpful to provide more context in log statements.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.Common.Logging
{
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Class that encapsulates the caller's (creator's) information.  This is helpful to provide more context in log statements.
    /// </summary>
    public class CallerInfo
    {
        /// <summary>
        /// The to string cache.
        /// </summary>
        private static readonly ConcurrentDictionary<int, string> ToStringCache =
            new ConcurrentDictionary<int, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CallerInfo"/> class.
        /// Creates a new instance of the CallerInfo class.
        /// </summary>
        /// <param name="memberName">
        /// The member Name.
        /// </param>
        /// <param name="filePath">
        /// The file Path.
        /// </param>
        /// <param name="lineNumber">
        /// The line Number.
        /// </param>
        public CallerInfo(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            this.MemberName = memberName;
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the name of the method or property of the caller.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Gets the full path of the source file of the caller.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the line number of the source file of the caller.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Get the hash code for this instance.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.MemberName.GetHashCode() ^ this.FilePath.GetHashCode() ^ this.LineNumber;
        }

        /// <summary>
        /// String representation of the caller's info.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return ToStringCache.GetOrAdd(
                this.GetHashCode(),
                hc => $"{this.MemberName},{Path.GetFileName(this.FilePath)}({this.LineNumber})");
        }
    }
}