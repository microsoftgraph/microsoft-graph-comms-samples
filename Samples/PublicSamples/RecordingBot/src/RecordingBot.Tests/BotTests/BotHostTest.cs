using NUnit.Framework;

namespace RecordingBot.Tests.BotTests
{
    [TestFixture]
    [Ignore("Ignoring test as it's used to development and should be ran on local machine.")]
    public class BotHostTest : TestBase
    {
        [Test]
        public void FirstUpBot()
        {
            // ToDo: write a proper test that does not depend on TestBase deriving from AppHost and starting a complete ASP.NET Web Host
            System.Threading.Thread.Sleep(-1);
        }
    }
}
