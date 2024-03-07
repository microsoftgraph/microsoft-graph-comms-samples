// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="ConsoleMainTest.cs" company="Microsoft">
//     Copyright © 2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.IO;

namespace RecordingBot.Tests.ConsoleTest
{
    /// <summary>
    /// Defines test class ConsoleMainTest.
    /// </summary>
    [TestFixture]
    public class ConsoleMainTest
    {
        /// <summary>
        /// The save
        /// </summary>
        private TextWriter _save;

        /// <summary>
        /// Setups the console test.
        /// </summary>
        [OneTimeSetUp]
        public void SetupConsoleTest()
        {
            _save = System.Console.Out;
        }

        /// <summary>
        /// Teardowns the console test.
        /// </summary>
        [OneTimeTearDown]
        public void TeardownConsoleTest()
        {
            System.Console.SetOut(_save);
        }


        /// <summary>
        /// Defines the test method TestVersion.
        /// </summary>
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
