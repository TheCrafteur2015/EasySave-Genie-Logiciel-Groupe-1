using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using EasySave.View.Localization;
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
            var allFiles = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);

            var filesToCopy = new List<string>();
            foreach (var file in allFiles)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, file);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var sourceFileInfo = new FileInfo(file);

                if (!File.Exists(targetFile) ||
                    sourceFileInfo.LastWriteTime > new FileInfo(targetFile).LastWriteTime ||
                    sourceFileInfo.Length != new FileInfo(targetFile).Length)
                {
                    filesToCopy.Add(file);
                }
            }

            var totalFiles = filesToCopy.Count;
            long totalSize = filesToCopy.Sum(f => new FileInfo(f).Length);

            if (totalFiles == 0)
            {
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Completed, ProgressPercentage = 100, Message = "Up to date!" });
                return;
            }

            var priorityFiles = filesToCopy.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = filesToCopy.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
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

            // --- 5. Backup Loop (Optimized) ---
            int filesSinceLastProcessCheck = 0;

            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));
                var sourceFileInfo = new FileInfo(sourceFile);
                var fileSize = sourceFileInfo.Length;

                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                // Optimisation : Vérification Logiciel Métier
                bool isBigFile = fileSize > 1024 * 1024;
                if (filesSinceLastProcessCheck >= 50 || isBigFile)
                {
                    while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                    {
                        progressCallback?.Invoke(new ProgressState
                        {
                            BackupName = job.Name,
                            State = State.Paused,
                            Message = $"Paused: {BusinessSoftware} running...",
                            TotalFiles = totalFiles,
                            TotalSize = totalSize,
                            FilesRemaining = totalFiles - processedFiles,
                            SizeRemaining = totalSize - processedSize
                        });
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
                        Thread.Sleep(50);
                        if (job.Cts.IsCancellationRequested) break;
                    }
                }

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var stopwatch = Stopwatch.StartNew();
                int encryptionTime = 0;
                bool semaphoreAcquired = false;

                try
                {
                    if (fileSize > maxFileSizeBytes)
                    {
                        BackupManager.BigFileSemaphore.Wait();
                        semaphoreAcquired = true;
                    }

                    // Copie optimisée
                    if (fileSize < 4 * 1024 * 1024)
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                    else
                    {
                        using (FileStream fsSource = new(sourceFile, FileMode.Open, FileAccess.Read))
                        using (FileStream fsDest = new(targetFile, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[4 * 1024 * 1024];
                            int bytesRead;
                            long lastUpdateTick = 0;
                            long currentFileCopied = 0;

                            while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (job.Cts.IsCancellationRequested) break;
                                job.PauseWaitHandle.Wait();
                                fsDest.Write(buffer, 0, bytesRead);
                                currentFileCopied += bytesRead;

                                long currentTick = DateTime.Now.Ticks;
                                if (currentTick - lastUpdateTick > 1000000)
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
                                        CurrentTargetFile = targetFile
                                    });
                                    lastUpdateTick = currentTick;
                                }
                            }
                        }
                    }

                    // CryptoSoft
                    string ext = Path.GetExtension(targetFile);
                    if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                    {
                        BackupManager.CryptoSoftMutex.WaitOne();
                        try
                        {
                            var p = Process.Start(new ProcessStartInfo(cryptoPath, $"\"{targetFile}\" \"{cryptoKey}\"") { CreateNoWindow = true });
                            p?.WaitForExit();
                            encryptionTime = p?.ExitCode ?? -1;
                        }
                        finally { BackupManager.CryptoSoftMutex.ReleaseMutex(); }
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
                    BackupManager.GetLogger().LogError(e);
                }
                finally
                {
                    if (semaphoreAcquired) BackupManager.BigFileSemaphore.Release();
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                }

                processedFiles++;
                processedSize += fileSize;

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
            else
            {
                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Error,
                    Message = EasySave.View.Localization.I18n.Instance.GetString("status_stopped") ?? "Stopped by user"
                });
            }
        }
    }
}