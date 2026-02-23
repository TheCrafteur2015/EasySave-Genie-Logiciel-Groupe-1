using EasyLog.Data;

namespace EasyLog.Logging
{
    /// <summary>
    /// Composite logger that writes log entries to multiple destinations simultaneously.
    /// </summary>
    /// <remarks>
    /// Implements the Composite pattern to allow logging to both local files and remote servers
    /// at the same time. This is typically used when the LogMode configuration is set to "Both".
    /// </remarks>
    public class CompositeLogger(ILogger localLogger, ILogger remoteLogger) : ILogger
    {
        private readonly ILogger _localLogger = localLogger ?? throw new ArgumentNullException(nameof(localLogger));
        private readonly ILogger _remoteLogger = remoteLogger ?? throw new ArgumentNullException(nameof(remoteLogger));

        /// <summary>
        /// Logs an entry to both local and remote destinations.
        /// </summary>
        /// <param name="entry">The log entry to write.</param>
        public void Log(LogEntry entry)
        {
            _localLogger.Log(entry);
            _remoteLogger.Log(entry);
        }
    }
}
