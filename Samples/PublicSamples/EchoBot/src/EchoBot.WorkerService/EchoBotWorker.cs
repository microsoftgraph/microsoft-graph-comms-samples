using EchoBot.Api;

namespace EchoBot.WorkerService
{
    public class EchoBotWorker : BackgroundService
    {
        private readonly ILogger<EchoBotWorker> _logger;

        public EchoBotWorker(ILogger<EchoBotWorker> logger)
        {
            _logger = logger;
        }

        //public override 

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    try
        //    {
        //        while (!stoppingToken.IsCancellationRequested)
        //        {
        //            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        //            var bot = new BotHost();
        //            bot.Start();

        //            await Task.Delay(1000, stoppingToken);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //    finally
        //    {
        //        await Task.Delay(TimeSpan.FromSeconds(1000));
        //    }
        //}

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // DO YOUR STUFF HERE

            var bot = new BotHost();
            bot.Start();

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // DO YOUR STUFF HERE
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("EchoBot Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override void Dispose()
        {
            // DO YOUR STUFF HERE
        }
    }
}