using EasyLog.Logging;

namespace EasyLog.Data
{
	//public record LogEntry(int Timestamp, string Name, string Source, string Target, long Size, long ElapsedTime) {}
	public class LogEntry
	{
		public string Timestamp { get; } = DateTime.Now.ToString();

		public Level Level { get; set; } = Level.Info;

		public string? Message { get; set; }

		public string? Stacktrace { get; set; }

		public string? Name { get; set; }
		
		public string? SourceFile { get; set; }
		
		public string? TargetFile { get; set; }
		
		public long? FileSize { get; set; }
		
		public long? ElapsedTime { get; set; }

        public int EncryptionTime { get; set; } = 0;

        public string ToBackupString()
		{
            return $"Backup name: {Name}, Source: {SourceFile}, Destination: {TargetFile}, Size: {FileSize}, ElapsedTime: {ElapsedTime}ms, EncryptionTime: {EncryptionTime}ms";
        }

        public override string ToString()
        {
			string body = string.Empty;
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
