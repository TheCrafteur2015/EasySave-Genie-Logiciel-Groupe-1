namespace EasyLog.Data
{
	//public record LogEntry(int Timestamp, string Name, string Source, string Target, long Size, long ElapsedTime) {}
	public class LogEntry
	{
		public string Timestamp { get; } = DateTime.Now.ToString();

		public string Level { get; set; } = Logging.Level.Info.ToString();

		public string? Message { get; set; }

		public string? Stacktrace { get; set; }

		public string? Name { get; set; }
		
		public string? SourceFile { get; set; }
		
		public string? TargetFile { get; set; }
		
		public long? FileSize { get; set; }
		
		public long? ElapsedTime { get; set; }

		public string ToBackupString()
		{
			return $"Backup name: {Name}, Source: {SourceFile}, Destination: {TargetFile}, Size: {FileSize}, ElapsedTime: {ElapsedTime}";
		}

        public override string ToString()
        {
			if (Name != null && SourceFile != null && TargetFile != null && FileSize != null && ElapsedTime != null)
				return ToBackupString();
			return Message ?? string.Empty;
        }

	}
}
