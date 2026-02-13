using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Complete backup strategy - copies all files
    /// </summary>
    public class CompleteBackupStrategy : IBackupStrategy
    {

        /// <summary>
        /// Executes the specified backup job, copying all files from the source directory to the target directory and
        /// reporting progress through a callback.
        /// </summary>
        /// <remarks>
        /// The method creates the target directory if it does not already exist. The progress callback is
        /// invoked periodically with the current progress state, including after the operation completes. If an error occurs
        /// while copying a file, the method logs the error and continues processing the remaining files.
        /// Paths are logged in UNC format.
        /// </remarks>
        /// <param name="job">The backup job to execute. Specifies the source and target directories, as well as backup metadata.</param>
        /// <param name="BusinessSoftware">The name of the business software to check for. If running, the backup may be aborted.</param>
        /// <param name="progressCallback">A callback method that receives progress updates as the backup operation proceeds. Can be null if progress
        /// reporting is not required.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the source directory specified in the backup job does not exist.</exception>
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

                job.State = State.Error;
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Error,
                    Message = msg
                });

                return;
            }

            int processedFiles = 0;
            long processedSize = 0;

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

                    job.State = State.Error;

                    progressCallback?.Invoke(new ProgressState
                    {
                        BackupName = job.Name,
                        State = State.Error,
                        Message = msg
                    });

                    break;
                }

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var fileInfo = new FileInfo(sourceFile);
                var fileSize = fileInfo.Length;

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
                }
                catch (Exception e)
                {
                    stopwatch.Stop();
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Error, Message = $"An exception occured while saving file: {sourceFile}" });
                    BackupManager.GetLogger().LogError(e);
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