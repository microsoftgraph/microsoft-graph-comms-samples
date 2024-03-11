using NUnit.Framework;
using System.IO;

namespace RecordingBot.Tests
{
    [SetUpFixture]
    public class TestFixture
    {
        [OneTimeSetUp]
        public void ChangeCurrentDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(TestFixture).Assembly.Location);
            Directory.SetCurrentDirectory(dir);
        }
    }
}
