using NUnit.Framework;
using RecordingBot.Model.Constants;
using System.IO;

namespace RecordingBot.Tests.AudioTest
{
    [TestFixture]
    public class AudioSampleData
    {
        private static readonly string _path = Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, "test", "audio");

        [TearDown]
        public void testClean()
        {
            Directory.Delete(_path, true);
        }

        [SetUp]
        public void testInit()
        {
            Directory.CreateDirectory(_path);
        }
    }
}
