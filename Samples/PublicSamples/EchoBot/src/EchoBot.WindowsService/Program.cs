using System.ServiceProcess;

namespace EchoBot.WindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new EchoBotService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
