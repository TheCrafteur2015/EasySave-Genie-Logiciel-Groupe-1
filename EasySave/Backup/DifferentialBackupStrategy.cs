using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Differential backup strategy - copies only files that are new or have been modified since the last backup.
    /// Implements the <see cref="IBackupStrategy"/> interface.
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        /// <summary>
        /// Executes the differential backup job, comparing source and target files to decide what to copy.
        /// </summary>
        /// <remarks>
        /// This method calculates the dynamic path for CryptoSoft, handles business software detection,
        /// manages file priorities, and ensures CryptoSoft remains a single-instance process via a global Mutex.
        /// </remarks>
        /// <param name="job">The backup job configuration containing source and target directories.</param>
        /// <param name="BusinessSoftware">The name of the business software process to check for before and during execution.</param>
        /// <param name="progressCallback">A callback used to report real-time progress updates to the UI.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the source directory does not exist.</exception>
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

            // 3. Prepare files and priorities
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();

            if (priorityFiles.Count > 0)
            {
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);
            }

            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            // 4. Initial Business Software check
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

            // 5. Differential backup loop
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // User Cancellation check
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }

                // Pause handling
                job.PauseWaitHandle.Wait();

                // Mid-execution Business Software check
                if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }

                // Priority coordination (Wait for pending priority files in other threads)
                if (!isPriority)
                {
                    while (BackupManager.GlobalPriorityFilesPending > 0)
                    {
                        Thread.Sleep(50);
                        if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0) break;
                    }
                }

                var sourceFileInfo = new FileInfo(sourceFile);
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // Differential logic: only copy if target doesn't exist or source is newer/different size
                bool needsCopy = !File.Exists(targetFile) ||
                                 sourceFileInfo.LastWriteTime > new FileInfo(targetFile).LastWriteTime ||
                                 sourceFileInfo.Length != new FileInfo(targetFile).Length;

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

                if (needsCopy)
                {
                    var stopwatch = Stopwatch.StartNew();
                    int encryptionTime = 0;
                    bool semaphoreAcquired = false;

                    try
                    {
                        // Large file bandwidth protection
                        if (sourceFileInfo.Length > ((long)(config.GetConfig("MaxParallelTransferSize") ?? 1000) * 1024))
                        {
                            BackupManager.BigFileSemaphore.Wait();
                            semaphoreAcquired = true;
                        }

                        File.Copy(sourceFile, targetFile, true);

                        // CryptoSoft Integration (Single-instance handling)
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

                        // Log Success (UNC Format)
                        BackupManager.GetLogger().Log(new LogEntry
                        {
                            Name = job.Name,
                            SourceFile = PathUtils.ToUnc(sourceFile),
                            TargetFile = PathUtils.ToUnc(targetFile),
                            FileSize = sourceFileInfo.Length,
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
                            FileSize = sourceFileInfo.Length,
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
                    }
                }

                // Decrement priority counter even if no copy was needed
                if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);

                processedFiles++;
                processedSize += sourceFileInfo.Length;
            }

            // 6. Final State Reporting
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