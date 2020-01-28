// <copyright file="HeartbeatHandlerTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Samples.Common.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Sample.Common;

    /// <summary>
    /// The heartbeat handler tests.
    /// </summary>
    [TestClass]
    public class HeartbeatHandlerTests
    {
        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        public TestContext TestContext
        {
            get; set;
        }

        /// <summary>
        /// Heartbeat should trigger.
        /// </summary>
        [TestMethod]
        public void HeartbeatShouldTrigger()
        {
            var logger = new GraphLogger(nameof(this.HeartbeatShouldTrigger));

            var handlerCount = 0;
            var handler = new TestHandler(TimeSpan.FromSeconds(1), logger, args =>
            {
                Interlocked.Increment(ref handlerCount);
                return Task.CompletedTask;
            });

            Thread.Sleep(TimeSpan.FromSeconds(3));
            Assert.IsTrue(handlerCount >= 2, $"handlerCount >= 2 failed: handlerCount = {handlerCount}");

            handler.Dispose();
            handlerCount = 0;

            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.AreEqual(0, handlerCount);

            handler.Dispose();
        }

        /// <summary>
        /// Heartbeat should log success.
        /// </summary>
        [TestMethod]
        public void HeartbeatShouldLogSuccess()
        {
            var formatter = new CommsLogEventFormatter();
            var logger = new GraphLogger(nameof(this.HeartbeatShouldLogSuccess));
            logger.DiagnosticLevel = TraceLevel.Verbose;

            var loggerCount = 0;
            var observer = new Observer<LogEvent>(
                logger,
                onNext: @event =>
                {
                    Interlocked.Increment(ref loggerCount);
                    this.TestContext.WriteLine(formatter.Format(@event));
                },
                onError: @exception =>
                {
                    Assert.Fail(@exception.ToString());
                });

            var handler = new TestHandler(TimeSpan.FromSeconds(1), logger, args =>
            {
                return Task.CompletedTask;
            });

            Thread.Sleep(TimeSpan.FromSeconds(4));
            Assert.IsTrue(loggerCount >= 4, $"loggerCount >= 4 failed: loggerCount = {loggerCount}");

            handler.Dispose();
        }

        /// <summary>
        /// Heartbeat should log failure.
        /// </summary>
        [TestMethod]
        public void HeartbeatShouldLogFailure()
        {
            var formatter = new CommsLogEventFormatter();
            var logger = new GraphLogger(nameof(this.HeartbeatShouldLogSuccess));
            logger.DiagnosticLevel = TraceLevel.Error;

            var errorCount = 0;
            var observer = new Observer<LogEvent>(
                logger,
                onNext: @event =>
                {
                    Interlocked.Increment(ref errorCount);
                    this.TestContext.WriteLine(formatter.Format(@event));
                },
                onError: @exception =>
                {
                    Assert.Fail(@exception.ToString());
                });

            var handler = new TestHandler(TimeSpan.FromSeconds(1), logger, args =>
            {
                throw new Exception("Something went wrong!!!");
            });

            Thread.Sleep(TimeSpan.FromSeconds(4));
            Assert.IsTrue(errorCount >= 2, $"errorCount >= 2 failed: errorCount = {errorCount}");

            handler.Dispose();
        }

        /// <summary>
        /// Test heartbeat handler.
        /// </summary>
        private class TestHandler : HeartbeatHandler
        {
            private readonly Func<ElapsedEventArgs, Task> callbackFunction;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestHandler"/> class.
            /// </summary>
            /// <param name="frequency">The frequency of the heartbeat.</param>
            /// <param name="logger">The graph logger.</param>
            /// <param name="callbackFunction">The function to call on heartbeat event.</param>
            public TestHandler(TimeSpan frequency, IGraphLogger logger, Func<ElapsedEventArgs, Task> callbackFunction)
                : base(frequency, logger)
            {
                this.callbackFunction = callbackFunction;
            }

            /// <inheritdoc/>
            protected override Task HeartbeatAsync(ElapsedEventArgs args)
            {
                return this.callbackFunction(args);
            }
        }
    }
}
