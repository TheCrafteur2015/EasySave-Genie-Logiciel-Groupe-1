using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyLog.Data;

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

		private readonly string _path;

		public string LogFile { get; private set; }

		public AbstractLogger(string path)
		{
			this._path = path;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			LogFile = this.GetFile();
		}

		/// <summary>
		/// Generates the full file path for the log file corresponding to the current date.
		/// </summary>
		/// <returns>A string containing the absolute path to the log file for today, with the file name formatted as "yyyy-MM-dd.log".</returns>
		public string GetFile() => Path.Combine(_path, $"{DateTime.Now:yyyy-MM-dd}." + GetExtension());

		public abstract string GetExtension();

		/// <summary>
		/// Writes a log entry with the specified severity level and message.
		/// </summary>
		/// <param name="level">The severity level of the log entry. Determines the importance and filtering of the log message.</param>
		/// <param name="message">The message to log. Cannot be null.</param>
        public abstract void Log(LogEntry message);

	}
}
