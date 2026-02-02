using EasySave.Interfaces;
using EasySave.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Utils
{
    /// <summary>
    /// JSON implementation of the logger
    /// </summary>
    public class JsonLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();

        public JsonLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void LogTransfer(string backupName, string sourceFile, string targetFile, long fileSize, long transferTime)
        {
            var logEntry = new LogEntry
            {
                BackupName = backupName,
                SourceFilePath = ConvertToUncPath(sourceFile),
                TargetFilePath = ConvertToUncPath(targetFile),
                FileSize = fileSize,
                TransferTimeMs = transferTime
            };

            WriteToLogFile(logEntry);
        }

        public void LogError(string backupName, string message, string sourceFile = "")
        {
            var logEntry = new LogEntry
            {
                BackupName = backupName,
                SourceFilePath = string.IsNullOrEmpty(sourceFile) ? string.Empty : ConvertToUncPath(sourceFile),
                TargetFilePath = string.Empty,
                FileSize = 0,
                TransferTimeMs = -1,
                ErrorMessage = message
            };

            WriteToLogFile(logEntry);
        }

        private void WriteToLogFile(LogEntry logEntry)
        {
            lock (_lockObject)
            {
                string logFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
                string logFilePath = Path.Combine(_logDirectory, logFileName);

                List<LogEntry> entries;

                if (File.Exists(logFilePath))
                {
                    string existingContent = File.ReadAllText(logFilePath);
                    entries = JsonConvert.DeserializeObject<List<LogEntry>>(existingContent) ?? new List<LogEntry>();
                }
                else
                {
                    entries = new List<LogEntry>();
                }

                entries.Add(logEntry);

                string jsonContent = JsonConvert.SerializeObject(entries, Formatting.Indented);
                File.WriteAllText(logFilePath, jsonContent);
            }
        }

        private string ConvertToUncPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // If it's already a UNC path, return as is
            if (path.StartsWith(@"\\"))
                return path;

            // Convert local path to UNC format
            try
            {
                string fullPath = Path.GetFullPath(path);
                
                // For local drives, convert to UNC format
                if (fullPath.Length >= 2 && fullPath[1] == ':')
                {
                    string driveLetter = fullPath.Substring(0, 1);
                    string pathWithoutDrive = fullPath.Substring(2);
                    return $@"\\{Environment.MachineName}\{driveLetter}${pathWithoutDrive}";
                }

                return fullPath;
            }
            catch
            {
                return path;
            }
        }
    }
}
