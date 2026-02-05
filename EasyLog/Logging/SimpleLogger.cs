namespace EasyLog.Logging
{
    /// <summary>
    /// Provides a simple file-based logger that writes log messages to a specified file in plain text format.
    /// </summary>
    /// <remarks>Log entries are appended to the file in a thread-safe manner. Each entry includes a
    /// timestamp, log level, and message. This logger is suitable for basic logging needs where structured or
    /// asynchronous logging is not required.</remarks>
    /// <param name="path">The file path where log entries will be written. If the file does not exist, it will be created.</param>
	public class SimpleLogger(string path) : AbstractLogger(path)
	{

        private readonly object _lock = new();

        /// <summary>
        /// Writes a log entry with the specified severity level and message to the log file.
        /// </summary>
        /// <remarks>This method appends the log entry to the file specified by the Path property. The log
        /// entry includes a timestamp, the log level, and the message. This method is thread-safe.</remarks>
        /// <param name="level">The severity level of the log entry. Determines the importance or type of the log message.</param>
        /// <param name="message">The message to log. This value can be any string describing the event or information to record.</param>
        public override void Log(Level level, string message)
        {
            lock (_lock)
            {
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {level}: {message}\n");
			}
        }

        /// <summary>
        /// Logs the specified exception as an error entry to the log file.
        /// </summary>
        /// <remarks>The error message and stack trace are both written to the log file. This method is
        /// thread-safe.</remarks>
        /// <param name="e">The exception to log. Cannot be null.</param>
        public override void LogError(Exception e)
        {
			lock (_lock)
            {
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {Level.Error}: {e.Message}\n");
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {Level.Error}: Stacktrace: {e.StackTrace}\n");
			}
		}

    }
}
