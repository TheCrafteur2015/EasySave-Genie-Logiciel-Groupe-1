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
	public abstract class AbstractLogger<T> : ILogger<T>
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
        public abstract void Log(Level level, T message);

		/// <summary>
		/// Logs the specified exception as an error entry.
		/// </summary>
		/// <param name="e">The exception to log. Cannot be null.</param>
        public abstract void LogError(Exception e);

		public virtual void Log(Level level, object message) => Log(level, (T) message);

		public Type? GetGenericType()
		{
			Type type = this.GetType();

			foreach (Type interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ILogger<>))
				{
					return interfaceType.GetGenericArguments()[0];
				}
			}
			return null;
		}


	}
}
