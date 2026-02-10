using System.Diagnostics;
using EasyLog.Logging;

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
		/// <remarks>The method creates the target directory if it does not already exist. The progress callback is
		/// invoked periodically with the current progress state, including after the operation completes. If an error occurs
		/// while copying a file, the method logs the error and continues processing the remaining files.</remarks>
		/// <param name="job">The backup job to execute. Specifies the source and target directories, as well as backup metadata.</param>
		/// <param name="progressCallback">A callback method that receives progress updates as the backup operation proceeds. Can be null if progress
		/// reporting is not required.</param>
		/// <exception cref="DirectoryNotFoundException">Thrown if the source directory specified in the backup job does not exist.</exception>
		public void Execute(BackupJob job, Action<ProgressState> progressCallback)
		{
			if (!Directory.Exists(job.SourceDirectory))
			{
				BackupManager.GetLogger().Log(Level.Warning, $"{job.Name} - Source directory does not exist: {job.SourceDirectory}");
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

			foreach (var sourceFile in files)
			{
				var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
				var targetFile = Path.Combine(job.TargetDirectory, relativePath);
				var targetDir = Path.GetDirectoryName(targetFile);

				if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
					Directory.CreateDirectory(targetDir);

				var fileInfo = new FileInfo(sourceFile);
				var fileSize = fileInfo.Length;

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

				// Copy file and measure time
				var stopwatch = Stopwatch.StartNew();

				try
				{
					File.Copy(sourceFile, targetFile, true);
					stopwatch.Stop();

					BackupManager.GetLogger().Log(Level.Info, $"Backup name: {job.Name}, Source: {sourceFile}, Destination: {targetFile}, Size: {fileSize}, ElapsedTime: {stopwatch.ElapsedMilliseconds}");
				}
				catch (Exception e)
				{
					stopwatch.Stop();
					BackupManager.GetLogger().Log(Level.Error, $"An exception occured while saving file: {sourceFile}");
					BackupManager.GetLogger().LogError(e);
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
