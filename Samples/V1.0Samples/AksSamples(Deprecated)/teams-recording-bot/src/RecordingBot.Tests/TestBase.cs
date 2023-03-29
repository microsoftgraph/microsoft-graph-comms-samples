// ***********************************************************************
// Assembly         : RecordingBot.Tests
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="TestBase.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using RecordingBot.Services.ServiceSetup;

namespace RecordingBot.Tests
{
    /// <summary>
    /// Class TestBase.
    /// Implements the <see cref="RecordingBot.Services.ServiceSetup.AppHost" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.ServiceSetup.AppHost" />
    public class TestBase : AppHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestBase" /> class.

        /// </summary>
        public TestBase()
        {
            Boot();
        }
    }
}
