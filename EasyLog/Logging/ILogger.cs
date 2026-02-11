namespace EasyLog.Logging
{

	public interface ILogger
	{
		void Log(Level level, object message);

		/// <summary>
		/// Logs an error during backup
		/// </summary>
		void LogError(Exception e);

		Type? GetGenericType();
	}

	/// <summary>
	/// Interface for logging implementations
	/// </summary>
	public interface ILogger<T> : ILogger
	{
		/// <summary>
		/// Logs a transfer operation
		/// </summary>
		void Log(Level level, T message);

	}
}
