// <copyright file="Startup.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using PsiBot.Service.Settings;
using PsiBot.Services.Bot;
using PsiBot.Services.Logging;

namespace PsiBot.Services
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("PsiBot", redirectToTrace: true));
            services.AddSingleton<InMemoryObserver, InMemoryObserver>();
            services.Configure<BotConfiguration>(Configuration.GetSection(nameof(BotConfiguration)));
            services.PostConfigure<BotConfiguration>(config => config.Initialize());
            services.AddSingleton<IBotService, BotService>(provider =>
            {
                var bot = new BotService(
                    provider.GetRequiredService<IGraphLogger>(),
                    provider.GetRequiredService<IOptions<BotConfiguration>>());
                bot.Initialize();
                return bot;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();
            //app.UseExceptionHandler();
            app.UseMvc();
        }
    }
}
