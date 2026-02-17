using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace EasySave.Backup
{
    /// <summary>
    /// Stratégie de sauvegarde différentielle - Copie uniquement les fichiers nouveaux ou modifiés.
    /// </summary>
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
        {
            // 1. Vérifications des répertoires
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // 2. Chargement de la configuration (Crypto + Priorités)
            var config = BackupManager.GetBM().ConfigManager;
            string rawPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";
            string cryptoPath = string.IsNullOrEmpty(rawPath)
                ? ""
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));

            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> priorityExtensions = extensionsArray?.ToObject<List<string>>() ?? [];

            // 3. Préparation des fichiers et gestion de la priorité globale
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            if (priorityFiles.Count > 0)
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);

            // 4. Attente active du logiciel métier AVANT démarrage (v3.0)
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"[PAUSE] Logiciel métier '{BusinessSoftware}' détecté. En attente..." });
                    logSentStart = true;
                }
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Active, Message = $"En pause : Attente de fermeture de {BusinessSoftware}..." });
                
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

            // 5. Boucle de sauvegarde différentielle
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // Vérification annulation / Pause manuelle
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                // Vérification logiciel métier PENDANT l'exécution (v3.0)
                bool logSentLoop = false;
                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    if (!logSentLoop)
                    {
                        BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"[PAUSE] Logiciel métier '{BusinessSoftware}' détecté. Mise en pause..." });
                        logSentLoop = true;
                    }
                    progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Active, Message = $"En pause : Attente de fermeture de {BusinessSoftware}..." });
                    Thread.Sleep(2000);
                    if (job.Cts.IsCancellationRequested) break;
                }

                // Coordination des priorités (Attendre que les autres jobs finissent leurs fichiers prioritaires)
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

                // --- LOGIQUE DIFFÉRENTIELLE ---
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
                        // Gestion des gros fichiers (Sémaphore)
                        if (sourceFileInfo.Length > ((long)(config.GetConfig("MaxParallelTransferSize") ?? 1000) * 1024))
                        {
                            BackupManager.BigFileSemaphore.Wait();
                            semaphoreAcquired = true;
                        }

                        File.Copy(sourceFile, targetFile, true);

                        // Intégration CryptoSoft (Mutex mono-instance - Feature)
                        string ext = Path.GetExtension(targetFile);
                        if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                        {
                            BackupManager.CryptoSoftMutex.WaitOne(); // Début zone critique Mutex
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
                                BackupManager.CryptoSoftMutex.ReleaseMutex(); // Libération Mutex
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
                        BackupManager.GetLogger().Log(new LogEntry { Level = Level.Error, Message = $"Copy failed: {e.Message}" });
                        BackupManager.GetLogger().LogError(e);
                    }
                    finally
                    {
                        if (semaphoreAcquired) BackupManager.BigFileSemaphore.Release();
                    }
                }

                // On décrémente le compteur de priorité même si la copie n'était pas nécessaire
                if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);

                processedFiles++;
                processedSize += sourceFileInfo.Length;
            }

            // 6. État final (Succès)
            if (job.State != State.Error)
            {
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Completed, ProgressPercentage = 100 });
            }
        }
    }
}