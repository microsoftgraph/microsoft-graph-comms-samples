// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="AudioSampleData.cs" company="Microsoft">
//     Copyright © 2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using RecordingBot.Model.Constants;
using System.IO;

namespace RecordingBot.Tests.AudioTest
{
    /// <summary>
    /// Defines test class AudioSampleData.
    /// </summary>
    [TestFixture]
    public class AudioSampleData
    {
        /// <summary>
        /// The path
        /// </summary>
        private static readonly string path = Path.Combine(Path.GetTempPath(), BotConstants.DefaultOutputFolder, "test", "audio");

        /// <summary>
        /// Tests the clean.
        /// </summary>
        [TearDown]
        public void TestClean()
        {
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Tests the initialize.
        /// </summary>
        [SetUp]
        public void TestInit()
        {
            Directory.CreateDirectory(path);
        }
    }
}
