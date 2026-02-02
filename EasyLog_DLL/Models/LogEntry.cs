using System;

namespace EasyLog_DLL.Models
{
    /// <summary>
    /// Represents a log entry for backup operations
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; } = string.Empty;
        public string SourceFilePath { get; set; } = string.Empty;
        public string TargetFilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }
        public string? ErrorMessage { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
        }
    }
}
