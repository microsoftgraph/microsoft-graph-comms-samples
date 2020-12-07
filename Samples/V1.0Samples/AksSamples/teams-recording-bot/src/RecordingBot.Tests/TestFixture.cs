// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="TestFixture.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System.IO;

namespace RecordingBot.Tests
{
    /// <summary>
    /// Class TestFixture.
    /// </summary>
    [SetUpFixture]
    public class TestFixture
    {
        /// <summary>
        /// Changes the current directory.
        /// </summary>
        [OneTimeSetUp]
        public void ChangeCurrentDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(TestFixture).Assembly.Location);
            Directory.SetCurrentDirectory(dir);
        }
    }
}
