namespace EasyLog.Logging
{
	public static class LoggerFactory
	{

		private static readonly Dictionary<string, Type> _loggers = new()
		{
			{ "text", typeof(SimpleLogger) },
			{ "json", typeof(JsonLogger)   },
			{ "xml",  typeof(XmlLogger)    },
		};

		public static ILogger CreateLogger(string type, string path)
		{
			Type loggerType = _loggers[type] ?? throw new ArgumentException("This logger type doesn't exists!");
			return (ILogger) (Activator.CreateInstance(loggerType, path) ?? throw new Exception("A logger couldn't have been initialized!"));
		}

	}
}