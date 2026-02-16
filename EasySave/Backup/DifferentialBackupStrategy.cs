using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Differential backup strategy - copies only modified files
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {

        /// <summary>
        /// Executes the specified backup job, copying files from the source directory to the target directory and reporting
        /// progress through a callback.
        /// </summary>
        /// <remarks>
        /// The method performs a differential backup, copying only files that are new or have changed since
        /// the last backup. The target directory is created if it does not exist. The progress callback is invoked multiple
        /// times during execution, including a final call when the backup is complete. 
        /// If an error occurs, it is logged with a negative elapsed time (-1).
        /// Paths are logged in UNC format.
        /// </remarks>
        /// <param name="job">The backup job to execute. Specifies the source and target directories, as well as job metadata.</param>
        /// <param name="BusinessSoftware">The name of the business software to check for. If running, the backup may be aborted.</param>
        /// <param name="progressCallback">A callback that receives progress updates as the backup operation proceeds. The callback is invoked with a <see
        /// cref="ProgressState"/> object representing the current state of the backup. Can be <see langword="null"/> if
        /// progress updates are not required.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the source directory specified in <paramref name="job"/> does not exist.</exception>
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }

            var config = BackupManager.GetBM().ConfigManager;
            string cryptoPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";
            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> extensions = extensionsArray?.ToObject<List<string>>() ?? [];

            if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                string msg = $"[BLOCK] Logiciel métier détecté : '{BusinessSoftware}'. Sauvegarde annulée.";
                BackupManager.GetLogger().Log(new LogEntry
                {
                    Level = Level.Warning,
                    Message = $"Backup {job.Name} aborted: Business software '{BusinessSoftware}' is running."
                });
                job.State = State.Paused;
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Paused,
                    Message = msg
                });
                throw new InvalidOperationException(msg);
            }

            int processedFiles = 0;
            long processedSize = 0;
            int copiedFiles = 0;

            foreach (var sourceFile in files)
            {
                if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    string msg = $"[STOP] Logiciel métier détecté : '{BusinessSoftware}'. Sauvegarde interrompue.";
                    BackupManager.GetLogger().Log(new LogEntry
                    {
                        Level = Level.Warning,
                        Message = $"Backup {job.Name} stopped: Business software '{BusinessSoftware}' detected during execution."
                    });

                    job.State = State.Paused;

                    progressCallback?.Invoke(new ProgressState
                    {
                        BackupName = job.Name,
                        State = State.Paused,
                        Message = msg
                    });

                    throw new InvalidOperationException(msg);
                }

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var sourceFileInfo = new FileInfo(sourceFile);
                var fileSize = sourceFileInfo.Length;
                bool needsCopy = false;

                // Check if file needs to be copied (differential logic)
                if (!File.Exists(targetFile))
                    needsCopy = true;
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
                    State = State.Active,
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
                    int encryptionTime = 0;

                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        string ext = Path.GetExtension(targetFile);
                        if (File.Exists(cryptoPath) && extensions.Contains(ext))
                        {
                            var p = new Process();
                            p.StartInfo.FileName = cryptoPath;
                            p.StartInfo.Arguments = $"\"{targetFile}\" \"{cryptoKey}\"";
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.Start();
                            p.WaitForExit();
                            encryptionTime = p.ExitCode;
                        }
                        stopwatch.Stop();

                        // Convert paths to UNC before logging
                        string uncSource = PathUtils.ToUnc(sourceFile);
                        string uncTarget = PathUtils.ToUnc(targetFile);

                        BackupManager.GetLogger().Log(new LogEntry
                        {
                            Name = job.Name,
                            SourceFile = uncSource,
                            TargetFile = uncTarget,
                            FileSize = fileSize,
                            ElapsedTime = stopwatch.ElapsedMilliseconds,
                            EncryptionTime = encryptionTime
                        });
                        copiedFiles++;
                    }
                    catch (Exception e)
                    {
                        stopwatch.Stop();

                        // Convert paths to UNC for the error log
                        string uncSource = PathUtils.ToUnc(sourceFile);
                        string uncTarget = PathUtils.ToUnc(targetFile);

                        // 1. Log the structured entry with negative time (-1) as per specification
                        BackupManager.GetLogger().Log(new LogEntry
                        {
                            Name = job.Name,
                            SourceFile = uncSource,
                            TargetFile = uncTarget,
                            FileSize = fileSize,
                            ElapsedTime = -1, // Indicates error
                            EncryptionTime = 0,
                            Level = Level.Error,
                            Message = $"Copy failed: {e.Message}"
                        });

                        // 2. Log the full stack trace for debugging
                        BackupManager.GetLogger().LogError(e);
                    }
                }

                processedFiles++;
                processedSize += fileSize;
            }

            // Final progress state
            if (job.State != State.Error)
            {
                var finalState = new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Completed,
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
}