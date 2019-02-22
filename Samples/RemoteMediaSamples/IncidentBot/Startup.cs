// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.Common.Logging;
    using Sample.IncidentBot.Bot;

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
        /// <param name="configuration">The configuration interface.</param>
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
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(this.observer)
                .AddSingleton<IGraphLogger>(this.logger)
                .AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddAzureAdBearer(options => this.Configuration.Bind("AzureAd", options));

            services
                .AddBot(options => this.Configuration.Bind("Bot", options))
                .AddMvc();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="loggerFactory">The logger of ILogger instance.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            this.logger.BindToILoggerFactory(loggerFactory);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseMiddleware<CallAffinityMiddleware>();

            // bypass the user-auth middleware for the incoming request of calls.
            app.UseWhen(
                context => !context.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString(HttpRouteConstants.OnIncomingRequestRoute)),
                appBuilder => appBuilder.UseAuthentication());

            app.UseMvc();
        }
    }
}
