using MediaLogLevel = Microsoft.Skype.Bots.Media.LogLevel;

namespace EchoBot.Bot
{
    /// <summary>
    /// The MediaPlatformLogger.
    /// </summary>
    public class BotMediaLogger : IBotMediaLogger
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLogger" /> class.
        /// </summary>
        /// <param name="logger">Graph logger.</param>
        public BotMediaLogger(ILogger<BotMediaLogger> logger)
        {
            _logger = logger;
        }

        public void WriteLog(MediaLogLevel level, string logStatement)
        {
            LogLevel logLevel;
            switch (level)
            {
                case MediaLogLevel.Error:
                    logLevel = LogLevel.Error;
                    break;
                case MediaLogLevel.Warning:
                    logLevel = LogLevel.Warning;
                    break;
                case MediaLogLevel.Information:
                    logLevel = LogLevel.Information;
                    break;
                case MediaLogLevel.Verbose:
                    logLevel = LogLevel.Trace;
                    break;
                default:
                    logLevel = LogLevel.Trace;
                    break;
            }

            this._logger.Log(logLevel, logStatement);
        }
    }
}
