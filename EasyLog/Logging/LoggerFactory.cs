namespace EasyLog.Logging
{
	/// <summary>
	/// Factory class responsible for creating logger instances.
	/// Implements the Factory Method pattern to decouple logger instantiation from usage.
	/// </summary>
	public static class LoggerFactory
	{

		private static readonly Dictionary<string, Type> _loggers = new()
		{
			{ "text", typeof(SimpleLogger) },
			{ "json", typeof(JsonLogger)   },
			{ "xml",  typeof(XmlLogger)    },
		};

		/// <summary>
		/// Creates and returns an instance of a logger based on the specified type.
		/// </summary>
		/// <param name="type">The type of logger to create (e.g., "text", "json", "xml").</param>
		/// <param name="path">The file path where the logger will write its output.</param>
		/// <returns>An instance implementing the <see cref="ILogger"/> interface.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified logger type is not found in the registry.</exception>
		/// <exception cref="Exception">Thrown when the logger instance could not be initialized.</exception>
		public static ILogger CreateLogger(string type, string path)
		{
			Type loggerType = _loggers[type] ?? throw new ArgumentException("This logger type doesn't exists!");
			return (ILogger)(Activator.CreateInstance(loggerType, path) ?? throw new Exception("A logger couldn't have been initialized!"));
		}

	}
}