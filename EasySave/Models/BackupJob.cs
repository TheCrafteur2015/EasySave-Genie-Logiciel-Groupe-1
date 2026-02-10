using EasySave.Strategies.Interfaces;
using System;

namespace EasySave.Models
{
    /// <summary>
    /// Represents a backup job configuration
    /// </summary>
    public class BackupJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public IBackupStrategy? Strategy { get; set; }
        public DateTime? LastExecution { get; set; }
        public BackupState State { get; set; } = BackupState.Inactive;

        public BackupJob()
        {
        }

        public BackupJob(int id, string name, string sourceDir, string targetDir, BackupType type)
        {
            Id = id;
            Name = name;
            SourceDirectory = sourceDir;
            TargetDirectory = targetDir;
            Type = type;
        }
    }

    /// <summary>
    /// Backup job types
    /// </summary>
    public enum BackupType
    {
        Complete,
        Differential
    }

    /// <summary>
    /// Backup job state
    /// </summary>
    public enum BackupState
    {
        Inactive,
        Active,
        Completed,
        Error
    }
}
