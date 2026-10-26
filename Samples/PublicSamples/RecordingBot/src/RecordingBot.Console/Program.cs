using RecordingBot.Services.ServiceSetup;
using System;
using System.Diagnostics;
using System.Reflection;

namespace RecordingBot.Console
{
    public class Program : AppHost
    {
        public static void Main(string[] args)
        {
            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            if (args.Length > 0 && args[0].Equals("-v"))
            {
                System.Console.WriteLine(info.FileVersion);
                return;
            }

            var bot = new Program();

            try
            {
                System.Console.WriteLine("RecordingBot: booting");

                bot.Boot(args);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
            }
        }

        public static void ExceptionHandler(Exception e)
        {
            System.Console.BackgroundColor = ConsoleColor.Black;
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine($"Unhandled exception: {e.Message}");
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("Exception Details:");
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            InnerExceptionHandler(e.InnerException);
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("press any key to exit...");
            System.Console.ReadKey();
        }

        private static void InnerExceptionHandler(Exception e)
        {
            if (e == null) return;
            System.Console.WriteLine(e.Message);
            InnerExceptionHandler(e.InnerException);
        }
    }
}
