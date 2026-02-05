using System;

namespace EasySave.Backup
{
    /// <summary>
    /// Represents the current state of a backup operation
    /// </summary>
    public class ProgressState
    {
        public string BackupName { get; set; }
        public DateTime Timestamp { get; set; }
        public State State { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int FilesRemaining { get; set; }
        public long SizeRemaining { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }
        public double ProgressPercentage { get; set; }

        public ProgressState()
        {
            BackupName        = string.Empty;
            Timestamp         = DateTime.Now;
            State             = State.Inactive;
            CurrentSourceFile = string.Empty;
            CurrentTargetFile = string.Empty;
        }
    }
}
