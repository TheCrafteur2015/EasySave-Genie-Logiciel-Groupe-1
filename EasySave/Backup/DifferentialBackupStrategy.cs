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
            // 1. Vérifications initiales
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // 2. Chargement de la configuration (Crypto + Priorités + Limites)
            var config = BackupManager.GetBM().ConfigManager;
            
            // Gestion chemin CryptoSoft
            string rawPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";
            string cryptoPath = string.IsNullOrEmpty(rawPath) 
                ? "" 
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));
            
            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            
            // Gestion Priorités
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> priorityExtensions = extensionsArray?.ToObject<List<string>>() ?? [];

            // Optimisation : Calcul de la limite de taille une seule fois (v3.0)
            long maxFileSizeConfig = (long)(config.GetConfig("MaxParallelTransferSize") ?? 1000000);
            long maxFileSizeBytes = maxFileSizeConfig * 1024;

            // 3. Préparation des fichiers et tri par priorité
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var priorityFiles = files.Where(f => priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var nonPriorityFiles = files.Where(f => !priorityExtensions.Contains(Path.GetExtension(f))).ToList();
            var sortedFiles = priorityFiles.Concat(nonPriorityFiles).ToList();

            if (priorityFiles.Count > 0)
                Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);

            // 4. Gestion Logiciel Métier - Attente Active (State = Paused)
            bool logSentStart = false;
            while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (!logSentStart)
                {
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"[PAUSE] Logiciel métier '{BusinessSoftware}' détecté. Sauvegarde en attente..." });
                    logSentStart = true;
                }
                
                // On met l'état en Paused pour que l'UI réagisse correctement (v3.0)
                job.State = State.Paused;
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Paused, Message = $"En attente : Fermeture de {BusinessSoftware}..." });
                
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

            // 5. Boucle de sauvegarde (Files sorted by priority)
            foreach (var sourceFile in sortedFiles)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // A. Contrôles Temps Réel (Pause/Stop Utilisateur)
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    // Nettoyage du compteur global pour les fichiers restants
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }
                job.PauseWaitHandle.Wait();

                // B. Re-vérification Logiciel Métier pendant la boucle
                while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    // 
                    progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Paused, Message = $"Logiciel métier détecté. Pause forcée..." });
                    Thread.Sleep(2000);
                    if (job.Cts.IsCancellationRequested) break;
                }

                // C. Attente de la Priorité Globale
                // Si je ne suis pas prioritaire, mais qu'il y a des fichiers prioritaires ailleurs -> J'attends.
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
                        // 1. Gestion des gros fichiers (Sémaphore)
                        if (sourceFileInfo.Length > maxFileSizeBytes)
                        {
                            BackupManager.BigFileSemaphore.Wait();
                            semaphoreAcquired = true;
                        }

                        // 2. Copie physique
                        File.Copy(sourceFile, targetFile, true);

                        // 3. CryptoSoft (Mutex mono-instance)
                        string ext = Path.GetExtension(targetFile);
                        if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                        {
                            // Feature Branch: Protection stricte via Mutex
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
                        
                        // v3.0 Logging (Detailed)
                        BackupManager.GetLogger().Log(new LogEntry {
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
                        // v3.0 Error Logging
                        BackupManager.GetLogger().Log(new LogEntry {
                            Name = job.Name,
                            SourceFile = PathUtils.ToUnc(sourceFile),
                            TargetFile = PathUtils.ToUnc(targetFile),
                            FileSize = sourceFileInfo.Length,
                            ElapsedTime = -1, // Convention v3.0 pour erreur
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

                // Libération systématique du compteur de priorité
                if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);

                processedFiles++;
                processedSize += sourceFileInfo.Length;
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