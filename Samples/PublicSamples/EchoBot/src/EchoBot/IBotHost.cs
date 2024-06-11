namespace EchoBot
{
    public interface IBotHost
    {
        Task StartAsync();

        Task StopAsync();
    }
}