// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot
{
    using System;
    using System.Diagnostics;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;
    using Topshelf;

    /// <summary>
    /// The program class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main function.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            Debugger.Launch();
            try
            {
                var rc = HostFactory.Run(x =>
                {
                    x.Service<IncidentWindowsService>(s =>
                    {
                        s.ConstructUsing(service => new IncidentWindowsService());
                        s.WhenStarted(service => service.Start(args));
                        s.WhenStopped(service => service.Stop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription("Incident Bot Service");
                    x.SetDisplayName("Incident Bot");
                    x.SetServiceName("IncidentBot");
                    x.StartAutomatically();
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
