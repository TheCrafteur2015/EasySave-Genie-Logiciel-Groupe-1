using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Implements a differential backup strategy.
    /// This strategy only copies files that have been modified since the last backup 
    /// by comparing timestamps and file sizes.
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        /// <summary>
        /// Executes the differential backup process for a specific job.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <param name="BusinessSoftware">The process name of the business software to monitor.</param>
        /// <param name="progressCallback">A callback to report real-time progress updates to the UI.</param>
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            // --- 1. Initial Validations ---
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new() { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // --- 2. Configuration Loading ---
            var config = BackupManager.GetBM().ConfigManager;
            string rawPath = config.GetConfig<string>("CryptoSoftPath");
            string cryptoPath = string.IsNullOrEmpty(rawPath) ? "" : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));
            string cryptoKey = config.GetConfig<string>("CryptoKey") ?? "Key";

            List<string> priorityExtensions = config.GetConfig<List<string>>("PriorityExtensions");

            // --- 3. File Preparation and Priority Management ---
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            long maxFileSizeConfig = config.GetConfig<long?>("MaxParallelTransferSize") ?? 1000000L;
            long maxFileSizeBytes = maxFileSizeConfig * 1024;

            if (priorityFiles.Count > 0)
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);

            int processedFiles = 0;
            long processedSize = 0;

            // --- 4. Business Software Monitoring - Active Waiting (Pre-execution) ---
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"[PAUSE] Business software '{BusinessSoftware}' detected. Backup waiting..." });
                    logSentStart = true;
                }

                job.State = State.Paused;
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Paused,
                    Message = $"Waiting for: {BusinessSoftware} closure...",
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    FilesRemaining = totalFiles,
                    SizeRemaining = totalSize
                });

                Thread.Sleep(2000);

                if (job.Cts.IsCancellationRequested)
                {
                    if (priorityFiles.Count > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);
                    job.State = State.Error;
                    return;
                }
            }

            // --- 5. Backup Loop ---
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // Real-time Controls (Pause/Stop)
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                // Business Software Re-verification during loop
                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    progressCallback?.Invoke(new ProgressState
                    {
                        BackupName = job.Name,
                        State = State.Paused,
                        Message = $"Business software detected. Forced pause...",
                        TotalFiles = totalFiles,
                        TotalSize = totalSize,
                        FilesRemaining = totalFiles - processedFiles,
                        SizeRemaining = totalSize - processedSize
                    });
                    Thread.Sleep(2000);
                    if (job.Cts.IsCancellationRequested) break;
                }

                // Global Priority Orchestration
                if (!isPriority)
                {
                    while (BackupManager.GlobalPriorityFilesPending > 0)
                    {
                        Thread.Sleep(50);
                        if (job.Cts.IsCancellationRequested) break;
                    }
                }

                var sourceFileInfo = new FileInfo(sourceFile);
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // --- DIFFERENTIAL LOGIC ---
                // File is copied only if it doesn't exist, has a newer timestamp, or a different size.
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
                        // Big File Handling (Semaphore)
                        if (sourceFileInfo.Length > maxFileSizeBytes)
                        {
                            BackupManager.BigFileSemaphore.Wait();
                            semaphoreAcquired = true;
                        }

                        // --- STREAM-BASED COPY (CHUNK BY CHUNK) ---
                        long currentFileCopied = 0;
                        byte[] buffer = new byte[4 * 1024 * 1024]; // 4MB Buffer
                        int bytesRead;
                        long lastUpdateTick = 0;

                        using (FileStream fsSource = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                        using (FileStream fsDest = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
                        {
                            while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (job.Cts.IsCancellationRequested) break;

                                job.PauseWaitHandle.Wait();

                                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                                {
                                    progressCallback?.Invoke(new ProgressState
                                    {
                                        BackupName = job.Name,
                                        State = State.Paused,
                                        Message = $"Paused: {BusinessSoftware}...",
                                        TotalFiles = totalFiles,
                                        TotalSize = totalSize,
                                        FilesRemaining = totalFiles - processedFiles,
                                        SizeRemaining = totalSize - (processedSize + currentFileCopied)
                                    });
                                    Thread.Sleep(2000);
                                    if (job.Cts.IsCancellationRequested) break;
                                }
                                if (job.Cts.IsCancellationRequested) break;

                                fsDest.Write(buffer, 0, bytesRead);
                                currentFileCopied += bytesRead;

                                // UI Throttling (~100ms) for fluidity
                                long currentTick = DateTime.Now.Ticks;
                                if (currentTick - lastUpdateTick > 1000000 || currentFileCopied == sourceFileInfo.Length)
                                {
                                    progressCallback?.Invoke(new ProgressState
                                    {
                                        BackupName = job.Name,
                                        State = State.Active,
                                        TotalFiles = totalFiles,
                                        TotalSize = totalSize,
                                        FilesRemaining = totalFiles - processedFiles,
                                        SizeRemaining = totalSize - (processedSize + currentFileCopied),
                                        CurrentSourceFile = sourceFile,
                                        CurrentTargetFile = targetFile,
                                        ProgressPercentage = 0 // Recalculated by the ViewModel
                                    });
                                    lastUpdateTick = currentTick;
                                }
                            }
                        }

                        // Encryption handling via CryptoSoft
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
                            Level = Level.Error,
                            Message = $"Copy failed: {e.Message}",
                            ElapsedTime = -1
                        });
                        BackupManager.GetLogger().LogError(e);
                    }
                    finally
                    {
                        if (semaphoreAcquired) BackupManager.BigFileSemaphore.Release();
                    }
                }

                // Systematic Priority counter release
                if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);

                processedFiles++;
                processedSize += sourceFileInfo.Length;
            }

            // --- 6. Final State ---
            if (job.State != State.Error)
            {
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Completed,
                    ProgressPercentage = 100
                });
            }
        }
    }
}