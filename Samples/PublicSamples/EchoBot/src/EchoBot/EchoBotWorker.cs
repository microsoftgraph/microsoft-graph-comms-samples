using EchoBot.Bot;

namespace EchoBot
{
    public class EchoBotWorker : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<EchoBotWorker> _logger;
        private readonly IBotHost _botHost;

        public EchoBotWorker(IHostApplicationLifetime hostApplicationLifetime, ILogger<EchoBotWorker> logger, IBotHost botHost)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _botHost = botHost;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await _botHost.StartAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
            finally 
            {
                _logger.LogInformation("Stopping Echo Bot");
                _hostApplicationLifetime.StopApplication();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Ensure that the bot terminates all calls and disposes of the client
            await _botHost.StopAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}