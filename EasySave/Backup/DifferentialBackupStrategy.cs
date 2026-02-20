using EasyLog.Data;
using EasyLog.Logging;
using EasySave.Utils;
using System.Diagnostics;

namespace EasySave.Backup
{
	/// <summary>
	/// Implements a differential backup strategy.
	/// This strategy only copies files that have been modified since the last backup 
	/// by comparing timestamps and file sizes.
	/// </summary>
	public class DifferentialBackupStrategy : BackupStrategy
	{
		/// <summary>
		/// Executes the differential backup process for a specific job.
		/// </summary>
		/// <param name="job">The backup job to execute.</param>
		/// <param name="BusinessSoftware">The process name of the business software to monitor.</param>
		/// <param name="progressCallback">A callback to report real-time progress updates to the UI.</param>
		public override void ExecuteCoreJob(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
		{
			int processedFiles = 0;
			long processedSize = 0;

			foreach (var sourceFile in _sortedFiles)
			{
				bool isPriority = _priorityExtensions.Contains(Path.GetExtension(sourceFile));

				// Real-time Controls (Pause/Stop)
				if (job.Cts.IsCancellationRequested)
				{
					job.State = State.Error;
					if (isPriority)
						Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);
					int remaining = _sortedFiles.Skip(processedFiles + 1).Count(f => _priorityExtensions.Contains(Path.GetExtension(f)));
					if (remaining > 0)
						Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -remaining);
					break;
				}
				job.PauseWaitHandle.Wait();

				// Business Software Re-verification during loop
				while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
				{
					progressCallback?.Invoke(new()
					{
						BackupName     = job.Name,
						State          = State.Paused,
						Message        = $"Business software detected. Forced pause...",
						TotalFiles     = _totalFiles,
						TotalSize      = _totalSize,
						FilesRemaining = _totalFiles - processedFiles,
						SizeRemaining  = _totalSize - processedSize
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

				progressCallback?.Invoke(new()
				{
					BackupName         = job.Name,
					State              = State.Active,
					TotalFiles         = _totalFiles,
					TotalSize          = _totalSize,
					FilesRemaining     = _totalFiles - processedFiles,
					SizeRemaining      = _totalSize - processedSize,
					CurrentSourceFile  = sourceFile,
					CurrentTargetFile  = targetFile,
					ProgressPercentage = (double)processedFiles / _totalFiles * 100
				});

				if (needsCopy)
				{
					var stopwatch = Stopwatch.StartNew();
					int encryptionTime = 0;
					bool semaphoreAcquired = false;

					try
					{
						// Big File Handling (Semaphore)
						if (sourceFileInfo.Length > _maxFileSizeBytes)
						{
							BackupManager.BigFileSemaphore.Wait();
							semaphoreAcquired = true;
						}

						// --- STREAM-BASED COPY (CHUNK BY CHUNK) ---
						long currentFileCopied = 0;
						byte[] buffer = new byte[4 * 1024 * 1024]; // 4MB Buffer
						int bytesRead;
						long lastUpdateTick = 0;

						using (FileStream fsSource = new(sourceFile, FileMode.Open, FileAccess.Read))
						using (FileStream fsDest = new(targetFile, FileMode.Create, FileAccess.Write))
						{
							while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
							{
								if (job.Cts.IsCancellationRequested) break;

								job.PauseWaitHandle.Wait();

								while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
								{
									progressCallback?.Invoke(new()
									{
										BackupName     = job.Name,
										State          = State.Paused,
										Message        = $"Paused: {BusinessSoftware}...",
										TotalFiles     = _totalFiles,
										TotalSize      = _totalSize,
										FilesRemaining = _totalFiles - processedFiles,
										SizeRemaining  = _totalSize - (processedSize + currentFileCopied)
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
									progressCallback?.Invoke(new()
									{
										BackupName         = job.Name,
										State              = State.Active,
										TotalFiles         = _totalFiles,
										TotalSize          = _totalSize,
										FilesRemaining     = _totalFiles - processedFiles,
										SizeRemaining      = _totalSize - (processedSize + currentFileCopied),
										CurrentSourceFile  = sourceFile,
										CurrentTargetFile  = targetFile,
										ProgressPercentage = 0 // Recalculated by the ViewModel
									});
									lastUpdateTick = currentTick;
								}
							}
						}
						// --- FIN STREAM ---

						// Encryption handling via CryptoSoft
						BackupStrategy.Encrypt(targetFile, _cryptoPath, _cryptoKey, ref encryptionTime);

						stopwatch.Stop();
						BackupManager.GetLogger().Log(new()
						{
							Name           = job.Name,
							SourceFile     = PathUtils.ToUnc(sourceFile),
							TargetFile     = PathUtils.ToUnc(targetFile),
							FileSize       = sourceFileInfo.Length,
							ElapsedTime    = stopwatch.ElapsedMilliseconds,
							EncryptionTime = encryptionTime
						});
					}
					catch (Exception e)
					{
						stopwatch.Stop();
						BackupManager.GetLogger().Log(new()
						{
							Level       = Level.Error,
							Message     = $"Copy failed: {e.Message}",
							ElapsedTime = -1
						});
						BackupManager.GetLogger().LogError(e);
					}
					finally
					{
						if (semaphoreAcquired)
							BackupManager.BigFileSemaphore.Release();
					}
				}

				// Systematic Priority counter release
				if (isPriority)
					Interlocked.Decrement(ref BackupManager.GlobalPriorityFilesPending);

				processedFiles++;
				processedSize += sourceFileInfo.Length;
			}
		}
	}
}