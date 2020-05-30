using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Communications.Common.Telemetry;
using Sample.Common.Logging;

namespace Sample.ReminderBot
{
    public class Startup
    {
        private readonly GraphLogger logger;
        private readonly SampleObserver observer;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.logger = new GraphLogger(typeof(Startup).Assembly.GetName().Name);
            this.observer = new SampleObserver(this.logger);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
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
