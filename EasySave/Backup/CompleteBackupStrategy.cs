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
            string rawPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";
            string cryptoPath = string.IsNullOrEmpty(rawPath) ? "" : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));
            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> priorityExtensions = extensionsArray?.ToObject<List<string>>() ?? [];

            // 3. Préparation et tri des fichiers (Gestion de la priorité)
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            long maxFileSizeConfig = (long)(config.GetConfig("MaxParallelTransferSize") ?? 1000000);
            long maxFileSizeBytes = maxFileSizeConfig * 1024;

            // Incrémentation du compteur global de priorité
            if (priorityFiles.Count > 0)
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);

            // 4. Attente active du logiciel métier (v3.0 improved state management)
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    string msgWait = $"[PAUSE] Logiciel métier '{BusinessSoftware}' détecté. En attente...";
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msgWait });
                    logSentStart = true;
                }

                // Important : On signale à l'UI que le job est en PAUSE (et pas juste Active avec un message)
                job.State = State.Paused;
                progressCallback?.Invoke(new ProgressState { 
                    BackupName = job.Name, 
                    State = State.Paused, 
                    Message = $"En attente de fermeture de : {BusinessSoftware}" 
                });

                Thread.Sleep(2000);

                // Si l'utilisateur annule pendant l'attente
                if (job.Cts.IsCancellationRequested)
                {
                    // Nettoyage critique du compteur de priorité pour éviter les deadlocks
                    if (priorityFiles.Count > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);
                    job.State = State.Error;
                    return;
                }
            }

            int processedFiles = 0;
            long processedSize = 0;

            // 5. Boucle de copie (sur sortedFiles pour respecter les priorités)
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // A. Gestion de l'annulation
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    
                    // Nettoyage des priorités restantes (Correction critique v3.0)
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                
                // B. Gestion de la pause utilisateur (Manuel)
                job.PauseWaitHandle.Wait();

                // C. Re-vérification du logiciel métier pendant la copie (Pause Auto)
                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    // 
                    progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Paused, Message = $"Logiciel métier détecté. Pause forcée..." });
                    Thread.Sleep(2000);
                    if (job.Cts.IsCancellationRequested) break;
                }

                // D. Attente de la priorité globale (Inter-Job synchronization)
                if (!isPriority)
                {
                    while (BackupManager.GlobalPriorityFilesPending > 0)
                    {
                        Thread.Sleep(50);
                        if (job.Cts.IsCancellationRequested) break;
                    }
                }

                // Préparation de la cible
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

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
                    // 1. Gestion des gros fichiers (Sémaphore)
                    if (isBigFile)
                    {
                        BackupManager.BigFileSemaphore.Wait();
                        semaphoreAcquired = true;
                    }

                    // 2. Copie
                    File.Copy(sourceFile, targetFile, true);

                    // 3. Chiffrement (Mutex mono-instance - Feature CryptoSoft)
                    string ext = Path.GetExtension(targetFile);
                    if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                    {
                        BackupManager.CryptoSoftMutex.WaitOne(); // Début zone critique
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
                            BackupManager.CryptoSoftMutex.ReleaseMutex(); // Fin zone critique
                        }
                    }

                    stopwatch.Stop();
                    
                    // Logging détaillé (v3.0 standard)
                    BackupManager.GetLogger().Log(new LogEntry {
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
                    BackupManager.GetLogger().Log(new LogEntry {
                        Level = Level.Error,
                        Message = $"Copy failed: {e.Message}",
                        ElapsedTime = -1 // Convention d'erreur
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
                progressCallback?.Invoke(new ProgressState {
                    BackupName = job.Name,
                    State = State.Completed,
                    ProgressPercentage = 100
                });
            }
        }
    }
}