
using EasyLog.Data;

namespace EasyLog.Logging
{
	public class LoggerContext(ILogger logger) : ILogger
	{

		private ILogger _logger = logger;

		public Type? GetGenericType() => _logger.GetGenericType();

		public void Log(Level level, object message)
		{
			//if (GetGenericType() == typeof(string) && message.GetType() == typeof(string))
			//	message = new LogEntry { Message = (string) message };
			if (message.GetType() == typeof(LogEntry))
			{
				var log = (LogEntry) message;
				log.Level = level.ToString();
				message = log;
			}
			_logger.Log(level, message);
		}

		public void LogError(Exception e) => _logger.LogError(e);


		public void SetLogger(ILogger logger) => _logger = logger;

	}
}
