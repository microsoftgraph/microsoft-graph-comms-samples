// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.TeamsBot
{
    using System;
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Teams bot \psi store converter command-line tool.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Display command-line parser errors.
        /// </summary>
        /// <param name="errors">Errors reported.</param>
        /// <returns>Success flag.</returns>
        private static int DisplayParseErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine("Errors:");
            var ret = 0;
            foreach (var error in errors)
            {
                Console.WriteLine($"{error}");
                if (error.StopsProcessing)
                {
                    ret = 1;
                }
            }

            return ret;
        }

        private static int Main(string[] args)
        {
            Console.WriteLine("Teams Bot Data Conversion Tool");
            try
            {
                return Parser.Default.ParseArguments<Verbs.Encode, Verbs.Split>(args)
                    .MapResult(
                        (Verbs.Encode opts) => Utility.EncodeImageStreams(opts.Store, opts.Path, opts.Output, opts.Quality),
                        (Verbs.Split opts) => Utility.SplitDictionaryStreams(opts.Store, opts.Path, opts.Output, opts.Quality),
                        DisplayParseErrors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}
