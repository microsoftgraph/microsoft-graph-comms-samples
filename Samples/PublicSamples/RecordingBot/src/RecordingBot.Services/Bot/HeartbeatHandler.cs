using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RecordingBot.Services.Bot
{
    public abstract class HeartbeatHandler : ObjectRootDisposable
    {
        private Timer heartbeatTimer;

        public HeartbeatHandler(TimeSpan frequency, IGraphLogger logger)
            : base(logger)
        {
            // initialize the timer
            var timer = new Timer(frequency.TotalMilliseconds)
            {
                Enabled = true,
                AutoReset = true
            };
            timer.Elapsed += HeartbeatDetected;
            heartbeatTimer = timer;
        }

        protected abstract Task HeartbeatAsync(ElapsedEventArgs args);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            heartbeatTimer.Elapsed -= HeartbeatDetected;
            heartbeatTimer.Stop();
            heartbeatTimer.Dispose();
        }

        private void HeartbeatDetected(object sender, ElapsedEventArgs args)
        {
            var task = $"{GetType().FullName}.{nameof(HeartbeatAsync)}(args)";
            GraphLogger.Verbose($"Starting running task: " + task);
            _ = Task.Run(() => HeartbeatAsync(args)).ForgetAndLogExceptionAsync(GraphLogger, task);
        }
    }
}
