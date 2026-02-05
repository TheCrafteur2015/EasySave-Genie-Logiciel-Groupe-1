using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.Logging
{
	/// <summary>
	/// Provides a base implementation for loggers that write log entries to a file system path.
	/// </summary>
	/// <remarks>This abstract class defines common functionality for file-based loggers, including directory
	/// initialization and file path management. Derived classes must implement the logging methods to specify how log
	/// entries are written. This class is not intended to be used directly; instead, inherit from it to create a custom
	/// logger.</remarks>
	public abstract class AbstractLogger : ILogger
	{

		/// <summary>
		/// Gets the file system path associated with this instance.
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Initializes a new instance of the AbstractLogger class using the specified file path.
		/// </summary>
		/// <param name="path">The file system path where log entries will be written. Cannot be null or empty.</param>
		public AbstractLogger(string path)
		{
			Path = path;
			Init();
		}

		/// <summary>
		/// Initializes the required directory structure if it does not already exist.
		/// </summary>
		private void Init()
		{
			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);
		}

		/// <summary>
		/// Generates the full file path for the log file corresponding to the current date.
		/// </summary>
		/// <returns>A string containing the absolute path to the log file for today, with the file name formatted as "yyyy-MM-dd.log".</returns>
		public string GetFile() => System.IO.Path.Combine(Path, $"{DateTime.Now:yyyy-MM-dd}.log");

		/// <summary>
		/// Writes a log entry with the specified severity level and message.
		/// </summary>
		/// <param name="level">The severity level of the log entry. Determines the importance and filtering of the log message.</param>
		/// <param name="message">The message to log. Cannot be null.</param>
        public abstract void Log(Level level, string message);

		/// <summary>
		/// Logs the specified exception as an error entry.
		/// </summary>
		/// <param name="e">The exception to log. Cannot be null.</param>
        public abstract void LogError(Exception e);

    }
}
