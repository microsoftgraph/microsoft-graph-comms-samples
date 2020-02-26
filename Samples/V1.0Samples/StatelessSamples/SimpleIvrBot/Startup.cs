// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace SimpleIvrBot
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.Common.Logging;

    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private readonly GraphLogger logger;
        private readonly SampleObserver observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Project configurations.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.logger = new GraphLogger(typeof(Startup).Assembly.GetName().Name);
            this.observer = new SampleObserver(this.logger);
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(this.observer)
                .AddSingleton<IGraphLogger>(this.logger);

            services
                .AddBot(options => this.Configuration.Bind("Bot", options))
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">App builder.</param>
        /// <param name="env">Hosting environment.</param>
        /// /// <param name="loggerFactory">The logger of ILogger instance.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            this.logger.BindToILoggerFactory(loggerFactory);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
