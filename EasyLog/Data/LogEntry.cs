using EasyLog.Logging;

namespace EasyLog.Data
{
	//public record LogEntry(int Timestamp, string Name, string Source, string Target, long Size, long ElapsedTime) {}

	/// <summary>
	/// Represents a single log entry containing details about an event or a backup operation.
	/// </summary>
	/// <remarks>
	/// This class is designed to handle different types of logs: standard messages, errors with stack traces,
	/// and specific backup operation details (source, target, size, duration).
	/// </remarks>
	public class LogEntry
	{
		/// <summary>
		/// Gets the timestamp of when the log entry was created.
		/// </summary>
		/// <remarks>Initialized automatically to the current system time.</remarks>
		public string Timestamp { get; } = DateTime.Now.ToString();

		/// <summary>
		/// Gets or sets the severity level of the log entry.
		/// </summary>
		public Level Level { get; set; } = Level.Info;

		/// <summary>
		/// Gets or sets the main text message of the log entry.
		/// </summary>
		public string? Message { get; set; }

		/// <summary>
		/// Gets or sets the stack trace associated with an error, if applicable.
		/// </summary>
		public string? Stacktrace { get; set; }

		/// <summary>
		/// Gets or sets the name of the backup job associated with this entry.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets the full path of the source file.
		/// </summary>
		public string? SourceFile { get; set; }

		/// <summary>
		/// Gets or sets the full path of the target (destination) file.
		/// </summary>
		public string? TargetFile { get; set; }

		/// <summary>
		/// Gets or sets the size of the file in bytes.
		/// </summary>
		public long? FileSize { get; set; }

		/// <summary>
		/// Gets or sets the time elapsed during the file transfer operation (in milliseconds).
		/// </summary>
		public long? ElapsedTime { get; set; }

		/// <summary>
		/// Gets or sets the time taken to encrypt the file (in milliseconds).
		/// </summary>
		/// <remarks>
		/// 0 means no encryption was performed.
		/// A negative value (e.g., -1) indicates an error during the encryption process.
		/// </remarks>
		public int EncryptionTime { get; set; } = 0;

		/// <summary>
		/// Formats the backup-specific properties into a structured string.
		/// </summary>
		/// <returns>A string containing the name, source, destination, size, elapsed time, and encryption time.</returns>
		public string ToBackupString()
		{
			return $"Backup name: {Name}, Source: {SourceFile}, Destination: {TargetFile}, Size: {FileSize}, ElapsedTime: {ElapsedTime}ms, EncryptionTime: {EncryptionTime}ms";
		}

		/// <summary>
		/// Returns a string representation of the current log entry.
		/// </summary>
		/// <remarks>
		/// The format changes based on the available data:
		/// <list type="bullet">
		/// <item>If backup data is present, it returns the formatted backup string.</item>
		/// <item>If a message and stacktrace are present, it formats it as an error log.</item>
		/// <item>Otherwise, it returns the simple message.</item>
		/// </list>
		/// </remarks>
		/// <returns>The formatted log string prefixed with the timestamp and level.</returns>
		public override string ToString()
		{
			string body;
			if (Name != null && SourceFile != null && TargetFile != null && FileSize != null && ElapsedTime != null)
				body = ToBackupString();
			else if (Message != null && Stacktrace != null)
			{
				Level = Level.Error;
				body = Message + "\n";
				body += $"[{Timestamp}] {Level}: Stacktrace: {Stacktrace}";
			}
			else
				body = Message ?? string.Empty;
			return $"[{Timestamp}] {Level}: {body}";
		}

	}
}