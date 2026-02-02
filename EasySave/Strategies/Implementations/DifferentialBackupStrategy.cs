using EasySave.Models;
using EasySave.Strategies.Interfaces;
using EasySave.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Strategies.Implementations
{
    /// <summary>
    /// Differential backup strategy - copies only modified files
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        private readonly ILogger _logger;

        public DifferentialBackupStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public void Execute(BackupJob job, Action<ProgressState> progressCallback)
        {
            if (!Directory.Exists(job.SourceDirectory))
            {
                _logger.LogError(job.Name, $"Source directory does not exist: {job.SourceDirectory}");
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
            {
                Directory.CreateDirectory(job.TargetDirectory);
            }

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }

            int processedFiles = 0;
            long processedSize = 0;
            int copiedFiles = 0;

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var sourceFileInfo = new FileInfo(sourceFile);
                var fileSize = sourceFileInfo.Length;
                bool needsCopy = false;

                // Check if file needs to be copied (differential logic)
                if (!File.Exists(targetFile))
                {
                    needsCopy = true;
                }
                else
                {
                    var targetFileInfo = new FileInfo(targetFile);
                    
                    // Copy if source is newer or size is different
                    if (sourceFileInfo.LastWriteTime > targetFileInfo.LastWriteTime ||
                        sourceFileInfo.Length != targetFileInfo.Length)
                    {
                        needsCopy = true;
                    }
                }

                // Update progress state
                var progressState = new ProgressState
                {
                    BackupName = job.Name,
                    State = "Active",
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    FilesRemaining = totalFiles - processedFiles,
                    SizeRemaining = totalSize - processedSize,
                    CurrentSourceFile = sourceFile,
                    CurrentTargetFile = targetFile,
                    ProgressPercentage = (double)processedFiles / totalFiles * 100
                };

                progressCallback?.Invoke(progressState);

                if (needsCopy)
                {
                    // Copy file and measure time
                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        stopwatch.Stop();

                        _logger.LogTransfer(job.Name, sourceFile, targetFile, fileSize, stopwatch.ElapsedMilliseconds);
                        copiedFiles++;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _logger.LogError(job.Name, ex.Message, sourceFile);
                    }
                }

                processedFiles++;
                processedSize += fileSize;
            }

            // Final progress state
            var finalState = new ProgressState
            {
                BackupName = job.Name,
                State = "Completed",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                FilesRemaining = 0,
                SizeRemaining = 0,
                ProgressPercentage = 100
            };

            progressCallback?.Invoke(finalState);
        }
    }
}
