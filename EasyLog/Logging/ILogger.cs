namespace EasyLog.Logging
{
	/// <summary>
	/// Interface for logging implementations
	/// </summary>
	public interface ILogger<T>
	{
		/// <summary>
		/// Logs a transfer operation
		/// </summary>
		void Log(Level level, T message);

		/// <summary>
		/// Logs an error during backup
		/// </summary>
		void LogError(Exception e);
		
	}
}
