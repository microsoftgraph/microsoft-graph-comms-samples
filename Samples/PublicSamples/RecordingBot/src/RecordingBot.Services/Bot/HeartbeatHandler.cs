using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RecordingBot.Services.Bot
{
    public abstract class HeartbeatHandler : ObjectRootDisposable
    {
        private readonly Timer _heartbeatTimer;

        public HeartbeatHandler(TimeSpan frequency, IGraphLogger logger)
            : base(logger)
        {
            // initialize the timer
            _heartbeatTimer = new Timer(frequency.TotalMilliseconds)
            {
                Enabled = true,
                AutoReset = true,
            };

            _heartbeatTimer.Elapsed += HeartbeatDetected;
        }

        protected abstract Task HeartbeatAsync(ElapsedEventArgs args);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _heartbeatTimer.Elapsed -= HeartbeatDetected;
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
        }

        private void HeartbeatDetected(object sender, ElapsedEventArgs args)
        {
            var task = $"{GetType().FullName}.{nameof(HeartbeatAsync)}(args)";

            GraphLogger.Verbose($"Starting running task: " + task);

            _ = Task.Run(() => HeartbeatAsync(args)).ForgetAndLogExceptionAsync(GraphLogger, task);
        }
    }
}
