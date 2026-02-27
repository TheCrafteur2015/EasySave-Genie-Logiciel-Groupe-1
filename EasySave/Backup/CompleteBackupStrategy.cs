using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Implements a full backup strategy. 
    /// Copies all files from the source directory to the target directory, 
    /// supporting encryption, priority file handling, and business software monitoring.
    /// </summary>
    public class CompleteBackupStrategy : IBackupStrategy
    {
        /// <summary>
        /// Executes the full backup process for a specific job.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <param name="BusinessSoftware">The process name of the business software to monitor.</param>
        /// <param name="progressCallback">A callback to report real-time progress updates to the UI.</param>
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            // --- 1. Basic Validations ---
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

            // --- 3. File Preparation and Sorting ---
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            long maxFileSizeConfig = config.GetConfig<long?>("MaxParallelTransferSize") ?? 1000000L;
            long maxFileSizeBytes = maxFileSizeConfig * 1024;

            if (priorityFiles.Count > 0)
            {
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);
            }

            int processedFiles = 0;
            long processedSize = 0;

            // --- 4. Active Waiting for Business Software (Pre-execution) ---
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    string msgWait = $"[PAUSE] Business software '{BusinessSoftware}' detected. Waiting for closure...";
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msgWait });
                    logSentStart = true;
                }

                progressCallback?.Invoke(new()
                {
                    BackupName = job.Name,
                    State = State.Paused,
                    Message = $"Waiting for: {BusinessSoftware}",
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

            // --- 5. Main Copy Loop (Optimized & Throttled) ---
            int filesSinceLastProcessCheck = 0;
            long lastUiUpdateTick = 0;

            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));
                var fileSize = new FileInfo(sourceFile).Length;

                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                if (filesSinceLastProcessCheck >= 50 || fileSize > 1024 * 1024 || processedFiles == 0)
                {
                    while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                    {
                        progressCallback?.Invoke(new() { BackupName = job.Name, State = State.Paused, Message = $"Paused: {BusinessSoftware}..." });
                        Thread.Sleep(2000);
                        if (job.Cts.IsCancellationRequested) break;
                    }
                    filesSinceLastProcessCheck = 0;
                }
                filesSinceLastProcessCheck++;

                if (!isPriority)
                {
                    while (BackupManager.GlobalPriorityFilesPending > 0)
                    {
                        Thread.Sleep(100);
                        if (job.Cts.IsCancellationRequested) break;
                    }
                }

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                try
                {
                    bool semaphoreAcquired = false;
                    if (fileSize > maxFileSizeBytes) { BackupManager.BigFileSemaphore.Wait(); semaphoreAcquired = true; }

                    // COPIE
                    if (fileSize < 2 * 1024 * 1024)
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                    else
                    {
                        using var fsSource = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
                        using var fsDest = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                        byte[] buffer = new byte[1024 * 1024]; // Buffer de 1Mo
                        int bytesRead;
                        long currentFileCopied = 0;

                        while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (job.Cts.IsCancellationRequested) break;
                            job.PauseWaitHandle.Wait();

                            fsDest.Write(buffer, 0, bytesRead);
                            currentFileCopied += bytesRead;

                            long innerTick = DateTime.Now.Ticks;
                            if (innerTick - lastUiUpdateTick > 2000000)
                            {
                                progressCallback?.Invoke(new ProgressState
                                {
                                    BackupName = job.Name,
                                    State = State.Active,
                                    TotalFiles = totalFiles,
                                    TotalSize = totalSize,
                                    FilesRemaining = totalFiles - processedFiles,
                                    SizeRemaining = totalSize - (processedSize + currentFileCopied),
                                    ProgressPercentage = totalSize > 0 ? ((double)(processedSize + currentFileCopied) / totalSize) * 100 : 100
                                });
                                lastUiUpdateTick = innerTick;
                            }
                        }
                    }

                    if (semaphoreAcquired) BackupManager.BigFileSemaphore.Release();

                    // D. CHIFFREMENT
                    int encTime = 0;
                    if (File.Exists(cryptoPath) && priorityExtensions.Contains(Path.GetExtension(targetFile)))
                    {
                        BackupManager.CryptoSoftMutex.WaitOne();
                        try
                        {
                            var p = Process.Start(new ProcessStartInfo(cryptoPath, $"\"{targetFile}\" \"{cryptoKey}\"") { CreateNoWindow = true });
                            p?.WaitForExit();
                            encTime = p?.ExitCode ?? -1;
                        }
                        finally { BackupManager.CryptoSoftMutex.ReleaseMutex(); }
                    }

                    // LOGGING
                    BackupManager.GetLogger().Log(new() { Name = job.Name, SourceFile = PathUtils.ToUnc(sourceFile), TargetFile = PathUtils.ToUnc(targetFile), FileSize = fileSize, ElapsedTime = 1, EncryptionTime = encTime });
                }

                catch (Exception) { /* Ignorer ou Logger erreur */ }
                finally { if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending); }

                processedFiles++;
                processedSize += fileSize;

                long currentTick = DateTime.Now.Ticks;
                if (currentTick - lastUiUpdateTick > 2000000 || processedFiles == totalFiles)
                {
                    progressCallback?.Invoke(new()
                    {
                        BackupName = job.Name,
                        State = State.Active,
                        TotalFiles = totalFiles,
                        TotalSize = totalSize,
                        FilesRemaining = totalFiles - processedFiles,
                        SizeRemaining = totalSize - processedSize,
                        ProgressPercentage = (double)processedFiles / totalFiles * 100
                    });
                    lastUiUpdateTick = currentTick;
                }
            }
        }
    }
}