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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                
                    var bot = new BotHost();
                    bot.Start();

                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(1000));
            }
        }
    }
}