using EasyLog.Data;

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
		private static readonly object _lock = new();

		public override string GetExtension() => "log";

		/// <summary>
		/// Writes a log entry with the specified severity level and message to the log file.
		/// </summary>
		/// <remarks>This method appends the log entry to the file specified by the Path property. The log
		/// entry includes a timestamp, the log level, and the message. This method is thread-safe.</remarks>
		/// <param name="level">The severity level of the log entry. Determines the importance or type of the log message.</param>
		/// <param name="message">The message to log. This value can be any string describing the event or information to record.</param>
		public override void Log(LogEntry message)
		{
			lock (_lock)
			{
				File.AppendAllText(LogFile, $"{message}\n");
			}
		}

	}
}