using System;

namespace EasySave.Models
{
    /// <summary>
    /// Represents the current state of a backup operation
    /// </summary>
    public class ProgressState
    {
        public string BackupName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string State { get; set; } = "Inactive";
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int FilesRemaining { get; set; }
        public long SizeRemaining { get; set; }
        public string CurrentSourceFile { get; set; } = string.Empty;
        public string CurrentTargetFile { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }

        public ProgressState()
        {
            Timestamp = DateTime.Now;
        }
    }
}
