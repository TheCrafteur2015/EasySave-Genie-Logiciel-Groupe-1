namespace LogServer.Models
{
    /// <summary>
    /// Log severity levels matching EasyLog.Data.Level
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Represents a centralized log entry received from remote EasySave clients.
    /// </summary>
    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public LogLevel Level { get; set; }
        public string? Message { get; set; }
        public string? Stacktrace { get; set; }
        public string? Name { get; set; }
        public string? SourceFile { get; set; }
        public string? TargetFile { get; set; }
        public long? FileSize { get; set; }
        public long? ElapsedTime { get; set; }
        public int EncryptionTime { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
