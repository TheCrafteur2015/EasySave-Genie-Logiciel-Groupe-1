using EasyLog.Data;

namespace EasyLog.Logging
{

	/// <summary>
	/// Interface for logging implementations
	/// </summary>
	public interface ILogger
	{

		/// <summary>
		/// Logs a transfer operation
		/// </summary>
		void Log(LogEntry message);

		/// <summary>
		/// Logs the specified exception as an error entry to the log file.
		/// </summary>
		/// <remarks>The error message and stack trace are both written to the log file. This method is
		/// thread-safe.</remarks>
		/// <param name="e">The exception to log.</param>
		virtual void LogError(Exception e) => Log(new LogEntry { Level = Level.Error, Message = e.Message, Stacktrace = e.StackTrace });
		
	}
}