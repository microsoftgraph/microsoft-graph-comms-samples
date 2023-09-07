using EchoBot.Api;
using System.Diagnostics;

namespace EchoBot.WorkerService
{
    public class EchoBotWorker : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<EchoBotWorker> _logger;

        private BotHost? _bot = null;

        public EchoBotWorker(IHostApplicationLifetime hostApplicationLifetime, ILogger<EchoBotWorker> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
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
            try
            {
                _bot = new BotHost();
                _bot.Start();
            }
            catch (Exception e)
            {
                var a = e;
                throw;
            }
            

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // DO YOUR STUFF HERE

            if (_bot != null)
            {
                await _bot.StopAsync();
            }

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("EchoBot Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);

                _hostApplicationLifetime.StopApplication();
            }
        }

        public override void Dispose()
        {
            // DO YOUR STUFF HERE
            _bot = null;
            this.Dispose();
        }
    }
}