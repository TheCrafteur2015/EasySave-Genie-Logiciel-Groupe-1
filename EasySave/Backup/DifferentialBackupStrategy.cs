using System.Diagnostics;
using EasyLog.Data;
using EasyLog.Logging;

namespace EasySave.Backup
{
	/// <summary>
	/// Differential backup strategy - copies only modified files
	/// </summary>
	public class DifferentialBackupStrategy : IBackupStrategy
	{

		/// <summary>
		/// Executes the specified backup job, copying files from the source directory to the target directory and reporting
		/// progress through a callback.
		/// </summary>
		/// <remarks>The method performs a differential backup, copying only files that are new or have changed since
		/// the last backup. The target directory is created if it does not exist. The progress callback is invoked multiple
		/// times during execution, including a final call when the backup is complete. This method is not
		/// thread-safe.</remarks>
		/// <param name="job">The backup job to execute. Specifies the source and target directories, as well as job metadata.</param>
		/// <param name="progressCallback">A callback that receives progress updates as the backup operation proceeds. The callback is invoked with a <see
		/// cref="ProgressState"/> object representing the current state of the backup. Can be <see langword="null"/> if
		/// progress updates are not required.</param>
		/// <exception cref="DirectoryNotFoundException">Thrown if the source directory specified in <paramref name="job"/> does not exist.</exception>
		public void Execute(BackupJob job, Action<ProgressState> progressCallback)
		{
			if (!Directory.Exists(job.SourceDirectory))
			{
				BackupManager.GetLogger().Log(new LogEntry { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
				throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
			}

			if (!Directory.Exists(job.TargetDirectory))
				Directory.CreateDirectory(job.TargetDirectory);

			var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
			var totalFiles = files.Length;
			long totalSize = 0;

			foreach (var file in files)
			{
				var fileInfo = new FileInfo(file);
				totalSize += fileInfo.Length;
			}

			int processedFiles = 0;
			long processedSize = 0;
			int copiedFiles = 0;

			foreach (var sourceFile in files)
			{
				var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
				var targetFile = Path.Combine(job.TargetDirectory, relativePath);
				var targetDir = Path.GetDirectoryName(targetFile);

				if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
					Directory.CreateDirectory(targetDir);

				var sourceFileInfo = new FileInfo(sourceFile);
				var fileSize = sourceFileInfo.Length;
				bool needsCopy = false;

				// Check if file needs to be copied (differential logic)
				if (!File.Exists(targetFile))
					needsCopy = true;
				else
				{
					var targetFileInfo = new FileInfo(targetFile);

					// Copy if source is newer or size is different
					if (sourceFileInfo.LastWriteTime > targetFileInfo.LastWriteTime ||
						sourceFileInfo.Length != targetFileInfo.Length)
					{
						needsCopy = true;
					}
				}

				// Update progress state
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

				if (needsCopy)
				{
					// Copy file and measure time
					var stopwatch = Stopwatch.StartNew();

					try
					{
						File.Copy(sourceFile, targetFile, true);
						stopwatch.Stop();

						BackupManager.GetLogger().Log(new LogEntry
						{
							Name        = job.Name,
							SourceFile  = sourceFile,
							TargetFile  = targetFile,
							FileSize    = fileSize,
							ElapsedTime = stopwatch.ElapsedMilliseconds
						});
						copiedFiles++;
					}
					catch (Exception e)
					{
						stopwatch.Stop();
						BackupManager.GetLogger().Log(new LogEntry
						{
							Level = Level.Error,
							Message = $"An exception occured while saving file: {sourceFile}"
						});
						BackupManager.GetLogger().LogError(e);
					}
				}

				processedFiles++;
				processedSize += fileSize;
			}

			// Final progress state
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
