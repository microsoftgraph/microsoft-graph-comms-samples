// ***********************************************************************
// Assembly         : RecordingBot.Console
// Author           : JasonTheDeveloper
// Created          : 08-28-2020
//
// Last Modified By : dannyg
// Last Modified On : 08-28-2020
// ***********************************************************************
// <copyright file="Program.cs" company="Microsoft Corporation">
//      Copyright ©  2020 Microsoft Corporation. All rights reserved.
//      //    Licensed under the MIT license. under the MIT license.
// </copyright>
// <summary></summary>
// ***********************************************************************
using RecordingBot.Services.ServiceSetup;
using System;
using System.Diagnostics;
using System.Reflection;

namespace RecordingBot.Console
{
    /// <summary>
    /// Class Program.
    /// Implements the <see cref="RecordingBot.Services.ServiceSetup.AppHost" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.ServiceSetup.AppHost" />
    public class Program : AppHost
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            if (args.Length > 0 && args[0].Equals("-v"))
            {
                System.Console.WriteLine(info.FileVersion);
                return;
            }

            var bot = new Program();

            try
            {
                System.Console.WriteLine("RecordingBot: booting");

                bot.Boot();
                bot.StartServer();

                System.Console.WriteLine("RecordingBot: running");
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
            }
        }

        /// <summary>
        /// The exception message formatter in the console window
        /// </summary>
        /// <param name="e">The e.</param>
        public static void ExceptionHandler(Exception e)
        {
            System.Console.BackgroundColor = ConsoleColor.Black;
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine($"Unhandled exception: {e.Message}");
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("Exception Details:");
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            InnerExceptionHandler(e.InnerException);
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("press any key to exit...");
            System.Console.ReadKey();
        }

        /// <summary>
        /// Inners the exception handler.
        /// </summary>
        /// <param name="e">The e.</param>
        private static void InnerExceptionHandler(Exception e)
        {
            if (e == null) return; // return to the caller method
            System.Console.WriteLine(e.Message);
            // Call recursively to output all inner exception messages
            InnerExceptionHandler(e.InnerException);
        }
    }
}
