namespace EasySave.Logger
{
	/// <summary>
	/// Interface for logging implementations
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Logs a transfer operation
		/// </summary>
		void Log(Level level, string message);

		/// <summary>
		/// Logs an error during backup
		/// </summary>
		void LogError(Exception e);
		
	}
}
