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
            // 1. Vérifications de base sur les dossiers
            if (!Directory.Exists(job.SourceDirectory))
            {
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            if (!Directory.Exists(job.TargetDirectory))
                Directory.CreateDirectory(job.TargetDirectory);

            // 2. Chargement de la configuration
            var config = BackupManager.GetBM().ConfigManager;

            // --- GESTION DU CHEMIN CRYPTOSOFT (CORRIGÉE) ---
            string rawPath = config.GetConfig("CryptoSoftPath")?.ToString() ?? "";

            // Construit le chemin absolu proprement, même si rawPath est relatif (ex: "..\..\CryptoSoft.exe")
            string cryptoPath = string.IsNullOrEmpty(rawPath)
                ? ""
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));

            // --- DEBUG : Affiche dans la console noire pour vérification ---
            Console.WriteLine($"[DEBUG] Raw Path from JSON: '{rawPath}'");
            Console.WriteLine($"[DEBUG] Final Computed Path: '{cryptoPath}'");
            Console.WriteLine($"[DEBUG] File Exists?: {File.Exists(cryptoPath)}");
            // -------------------------------------------------------------

            string cryptoKey = config.GetConfig("CryptoKey")?.ToString() ?? "Key";
            var extensionsArray = config.GetConfig("PriorityExtensions") as JArray;
            List<string> priorityExtensions = extensionsArray?.ToObject<List<string>>() ?? [];

            // 3. Préparation des fichiers
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

            // 4. Vérification Logiciel Métier (Avant lancement)
            if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
            {
                if (priorityFiles.Count > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);

                string msg = $"[BLOCK] Logiciel métier détecté : '{BusinessSoftware}'.";
                BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msg });
                job.State = State.Error;
                progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Error, Message = msg });
                return;
            }

            int processedFiles = 0;
            long processedSize = 0;

            // 5. Boucle de copie
            foreach (var sourceFile in files)
            {
                bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile));

                // A. Gestion Annulation / Pause
                if (job.Cts.IsCancellationRequested)
                {
                    job.State = State.Error;
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"Job {job.Name} stopped by user." });
                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1)
                               .Count(f => priorityExtensions.Contains(Path.GetExtension(f)));

                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
                    break;
                }

                job.PauseWaitHandle.Wait();

                // B. Vérification Logiciel Métier (Pendant copie)
                if (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
                {
                    string msg = $"[STOP] Logiciel métier détecté : '{BusinessSoftware}'.";
                    BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = msg });
                    job.State = State.Error;
                    progressCallback?.Invoke(new ProgressState { BackupName = job.Name, State = State.Error, Message = msg });

                    if (isPriority) Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    int remaining = sortedFiles.Skip(processedFiles + 1).Count(f => priorityExtensions.Contains(Path.GetExtension(f)));
                    if (remaining > 0) Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);

                    break;
                }

                // C. Gestion Priorité (Attente des fichiers prioritaires)
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

                var fileInfo = new FileInfo(sourceFile);
                var fileSize = fileInfo.Length;

                // D. Mise à jour progression UI
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

                // E. Copie et Cryptage
                var stopwatch = Stopwatch.StartNew();
                int encryptionTime = 0;
                bool isBigFile = fileSize > maxFileSizeBytes;
                bool semaphoreAcquired = false;

                try
                {
                    // Gestion gros fichiers (Semaphore)
                    if (isBigFile)
                    {
                        BackupManager.BigFileSemaphore.Wait();
                        semaphoreAcquired = true;
                    }

                    File.Copy(sourceFile, targetFile, true);

                    // --- CRYPTOSOFT (MONO-INSTANCE) ---
                    string ext = Path.GetExtension(targetFile);
                    if (File.Exists(cryptoPath) && priorityExtensions.Contains(ext))
                    {
                        // On verrouille l'accès global à CryptoSoft
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
                            // On libère toujours le Mutex pour ne pas bloquer les autres
                            BackupManager.CryptoSoftMutex.ReleaseMutex();
                        }
                    }
                    // ----------------------------------

                    stopwatch.Stop();

                    // Log Succès
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
                    string uncSource = PathUtils.ToUnc(sourceFile);
                    string uncTarget = PathUtils.ToUnc(targetFile);

                    // Log Erreur
                    BackupManager.GetLogger().Log(new LogEntry
                    {
                        Name = job.Name,
                        SourceFile = uncSource,
                        TargetFile = uncTarget,
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
                    if (semaphoreAcquired)
                    {
                        BackupManager.BigFileSemaphore.Release();
                    }
                    if (isPriority)
                    {
                        Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
                    }
                }

                processedFiles++;
                processedSize += fileSize;
            }

            // 6. Fin du job
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