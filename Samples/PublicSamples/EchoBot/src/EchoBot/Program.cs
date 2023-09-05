// See https://aka.ms/new-console-template for more information
using EchoBot.Api;

Console.WriteLine("Hello, World!");

try
{
    
    //var builder = new ConfigurationBuilder();

    //builder
    //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    //    .AddEnvironmentVariables();

    //var app = builder.Build();
    //var asdf = app.GetSection("AppSettings");

    DotNetEnv.Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));
    var brennen = DotNetEnv.Env.GetString("AppSettings__ServiceDnsName", "Variable not found");
    Console.WriteLine(brennen);
    var bot = new BotHost();
    bot.Start();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("press any key to exit...");
}
finally
{
    await Task.Delay(TimeSpan.FromMilliseconds(1000));
}