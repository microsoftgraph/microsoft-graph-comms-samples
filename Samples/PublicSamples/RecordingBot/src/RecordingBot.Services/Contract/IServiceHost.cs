using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecordingBot.Services.ServiceSetup;
using System;

namespace RecordingBot.Services.Contract
{
    public interface IServiceHost
    {
        IServiceCollection Services { get; }
        IServiceProvider ServiceProvider { get; }
        ServiceHost Configure(IServiceCollection services, IConfiguration configuration);
        IServiceProvider Build();
    }
}
