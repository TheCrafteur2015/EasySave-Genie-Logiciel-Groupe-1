using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    public class CompleteBackupStrategy : IBackupStrategy
    {
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            // 1. Vérifications de base
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // 2. Chargement de la configuration
            var config = BackupManager.GetBM().ConfigManager;
            string rawPath = config.GetConfig<string>("CryptoSoftPath");
            string cryptoPath = string.IsNullOrEmpty(rawPath) ? "" : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));
            string cryptoKey = config.GetConfig<string>("CryptoKey") ?? "Key";

            List<string> priorityExtensions = config.GetConfig<List<string>>("PriorityExtensions");

            // 3. Préparation et tri des fichiers
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

            // 4. Attente active du logiciel métier
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    string msgWait = $"[PAUSE] Logiciel métier '{BusinessSoftware}' détecté. En attente...";
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msgWait });
                    logSentStart = true;
                }

                progressCallback?.Invoke(new ProgressState
                {
                    BackupName = job.Name,
                    State = State.Paused,
                    Message = $"En attente de fermeture de : {BusinessSoftware}"
                });

                Thread.Sleep(2000);

                if (job.Cts.IsCancellationRequested)
                {
                    if (priorityFiles.Count > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);
                    job.State = State.Error;
                    return;
                }
            }

            int processedFiles = 0;
            long processedSize = 0;

            // 5. Boucle de copie
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Paused, Message = $"Logiciel métier détecté. Pause forcée..." });
                    Thread.Sleep(2000);
                    if (job.Cts.IsCancellationRequested) break;
                }

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

                var fileSize = new FileInfo(sourceFile).Length;

                // Mise à jour initiale avant copie
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
                    ProgressPercentage = 0
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

                    // --- NOUVEAU : COPIE PAR STREAM (CHUNK BY CHUNK) ---
                    // Remplace File.Copy pour permettre la progression fluide

                    long currentFileCopied = 0;
                    byte[] buffer = new byte[4 * 1024 * 1024]; // Buffer de 4 Mo
                    int bytesRead;
                    long lastUpdateTick = 0;

                    using (FileStream fsSource = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    using (FileStream fsDest = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
                    {
                        while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Vérifications pause/cancel pendant la copie
                            if (job.Cts.IsCancellationRequested) break;
                            job.PauseWaitHandle.Wait();

                            // Vérification Logiciel métier (Arrêt immédiat V3.0)
                            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                            {
                                progressCallback?.Invoke(new ProgressState
                                {
                                    BackupName = job.Name,
                                    State = State.Paused,
                                    Message = $"Pause : {BusinessSoftware}..."
                                });
                                Thread.Sleep(2000);
                                if (job.Cts.IsCancellationRequested) break;
                            }
                            if (job.Cts.IsCancellationRequested) break;

                            // Écriture
                            fsDest.Write(buffer, 0, bytesRead);
                            currentFileCopied += bytesRead;

                            // Mise à jour UI (Throttling ~100ms pour fluidité)
                            long currentTick = DateTime.Now.Ticks;
                            if (currentTick - lastUpdateTick > 1000000 || currentFileCopied == fileSize)
                            {
                                progressCallback?.Invoke(new ProgressState
                                {
                                    BackupName = job.Name,
                                    State = State.Active,
                                    TotalFiles = totalFiles,
                                    TotalSize = totalSize,
                                    FilesRemaining = totalFiles - processedFiles,
                                    // CALCUL DYNAMIQUE DU RESTE À FAIRE EN OCTETS
                                    SizeRemaining = totalSize - (processedSize + currentFileCopied),
                                    CurrentSourceFile = sourceFile,
                                    CurrentTargetFile = targetFile,
                                    ProgressPercentage = 0 // Ignoré, le ViewModel recalcule
                                });
                                lastUpdateTick = currentTick;
                            }
                        }
                    }
                    // ---------------------------------------------------

                    // CryptoSoft après la copie (si nécessaire)
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
                        Level = Level.Error,
                        Message = $"Copy failed: {e.Message}",
                        ElapsedTime = -1
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

            // 6. État final
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