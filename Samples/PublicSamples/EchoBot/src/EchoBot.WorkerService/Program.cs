using EchoBot.WorkerService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<EchoBotWorker>();
    })
    .Build();

await host.RunAsync();
