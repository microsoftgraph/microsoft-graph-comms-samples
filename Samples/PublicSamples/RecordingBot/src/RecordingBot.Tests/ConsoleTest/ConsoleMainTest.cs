using NUnit.Framework;
using System;
using System.IO;

namespace RecordingBot.Tests.ConsoleTest
{
    [TestFixture]
    public class ConsoleMainTest
    {
        private TextWriter _save;

        [OneTimeSetUp]
        public void SetupConsoleTest()
        {
            _save = System.Console.Out;
        }

        [OneTimeTearDown]
        public void TeardownConsoleTest()
        {
            System.Console.SetOut(_save);
        }


        [Test]
        public void TestVersion()
        {
            using (StringWriter sw = new())
            {
                System.Console.SetOut(sw);

                Console.Program.Main(["-v"]);

                _ = Version.TryParse(sw.ToString(), out Version version);

                Assert.That(version, Is.Not.Null);
            }
        }
    }
}
