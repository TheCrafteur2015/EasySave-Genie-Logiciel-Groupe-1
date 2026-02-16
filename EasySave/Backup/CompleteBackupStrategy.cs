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
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            // 1. Basic directory checks
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // 2. Load configuration
            var config = BackupManager.GetBM().ConfigManager;

            // Resolve CryptoSoft path dynamically using the base directory
            string rawPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";
            string cryptoPath = string.IsNullOrEmpty(rawPath)
                ? ""
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));

            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> priorityExtensions = extensionsArray?.ToObject<List<string>>() ?? [];

            // 3. Prepare files
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();

            long maxFileSizeConfig = (long)(config.GetConfig("MaxParallelTransferSize") ?? 1000000);
            long maxFileSizeBytes = maxFileSizeConfig * 1024;

            if (priorityFiles.Count > 0)
            {
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);
            }

            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            // 4. Business Software check (Before starting)
            if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (priorityFiles.Count > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);

                string msg = $"[BLOCK] Business software detected: '{BusinessSoftware}'.";
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msg });
                job.State = State.Error;
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Error, Message = msg });
                return;
            }

            int processedFiles = 0;
            long processedSize = 0;

            // 5. Copy loop
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // Cancellation / Pause handling
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }

                job.PauseWaitHandle.Wait();

                // Business Software check (During copy)
                if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }

                // Priority Management (Wait for priority files in other jobs)
                if (!isPriority)
                {
                    while (BackupManager.GlobalPriorityFilesPending > 0)
                    {
                        Thread.Sleep(50);
                        if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0) break;
                    }
                }

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var fileSize = new FileInfo(sourceFile).Length;

                progressCallback?.Invoke(new ProgressState
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
                });

                var stopwatch = Stopwatch.StartNew();
                int encryptionTime = 0;
                bool isBigFile = fileSize > maxFileSizeBytes;
                bool semaphoreAcquired = false;

                try
                {
                    if (isBigFile)
                    {
                        BackupManager.BigFileSemaphore.Wait();
                        semaphoreAcquired = true;
                    }

                    File.Copy(sourceFile, targetFile, true);

                    // Encryption (Single-instance handling via Mutex)
                    string ext = Path.GetExtension(targetFile);
                    if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                    {
                        BackupManager.CryptoSoftMutex.WaitOne();
                        try
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
                        finally
                        {
                            BackupManager.CryptoSoftMutex.ReleaseMutex();
                        }
                    }

                    stopwatch.Stop();

                    BackupManager.GetLogger().Log(new LogEntry
                    {
                        Name = job.Name,
                        SourceFile = PathUtils.ToUnc(sourceFile),
                        TargetFile = PathUtils.ToUnc(targetFile),
                        FileSize = fileSize,
                        ElapsedTime = stopwatch.ElapsedMilliseconds,
                        EncryptionTime = encryptionTime
                    });
                }
                catch (Exception e)
                {
                    stopwatch.Stop();
                    BackupManager.GetLogger().Log(new LogEntry
                    {
                        Name = job.Name,
                        SourceFile = PathUtils.ToUnc(sourceFile),
                        TargetFile = PathUtils.ToUnc(targetFile),
                        FileSize = fileSize,
                        ElapsedTime = -1,
                        EncryptionTime = 0,
                        Level = Level.Error,
                        Message = $"Copy failed: {e.Message}"
                    });
                    BackupManager.GetLogger().LogError(e);
                }
                finally
                {
                    if (semaphoreAcquired) BackupManager.BigFileSemaphore.Release();
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                }

                processedFiles++;
                processedSize += fileSize;
            }

            // 6. Final State
            if (job.State != State.Error)
            {
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Completed,
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    FilesRemaining = 0,
                    SizeRemaining = 0,
                    ProgressPercentage = 100
                });
            }
        }
    }
}