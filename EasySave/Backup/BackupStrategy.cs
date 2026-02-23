using EasyLog.Logging;
using System.Diagnostics;

namespace EasySave.Backup
{
	/// <summary>
	/// Strategy interface for backup implementations (Strategy Pattern)
	/// </summary>
	public abstract class BackupStrategy
	{
		protected List<string> _sortedFiles = [];
		protected List<string> _priorityExtensions = [];

		protected string _cryptoPath = string.Empty;
		protected string _cryptoKey = string.Empty;

		protected int _totalFiles;
		protected long _totalSize;
		protected long _maxFileSizeBytes;

		/// <summary>
		/// Executes the backup strategy
		/// </summary>
		public void Execute(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback)
		{
			var bm     = BackupManager.GetBM();
			var logger = BackupManager.GetLogger();

			// --- 1. Initial Validations ---
			if (!Directory.Exists(job.SourceDirectory))
			{
				logger.Log(new() { Level = Level.Warning, Message = $"{job.Name} - Source directory does not exist: {job.SourceDirectory}" });
				throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
			}

			if (!Directory.Exists(job.TargetDirectory))
				Directory.CreateDirectory(job.TargetDirectory);

			// --- 2. Configuration Loading ---
			var config = bm.ConfigManager;
			string rawPath = config.GetConfig<string>("CryptoSoftPath");
			_cryptoPath = string.IsNullOrEmpty(rawPath) ? "" : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));
			_cryptoKey = config.GetConfig<string>("CryptoKey") ?? "Key";

			_priorityExtensions = config.GetConfig<List<string>>("PriorityExtensions");

			// --- 3. File Preparation and Sorting ---
			var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
			_totalFiles = files.Length;
			_totalSize = files.Sum(f => new FileInfo(f).Length);

			var priorityFiles = files.Where(f => _priorityExtensions.Contains(Path.GetExtension(f))).ToList();
			var nonPriorityFiles = files.Where(f => !_priorityExtensions.Contains(Path.GetExtension(f))).ToList();
			_sortedFiles = [.. priorityFiles, .. nonPriorityFiles];

			long maxFileSizeConfig = config.GetConfig<long?>("MaxParallelTransferSize") ?? 1000000L;
			_maxFileSizeBytes = maxFileSizeConfig * 1024;

			if (priorityFiles.Count > 0)
			{
				Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, priorityFiles.Count);
			}

			// --- 4. Active Waiting for Business Software (Pre-execution) ---
			bool logSentStart = false;
			while (!string.IsNullOrEmpty(BusinessSoftware) && Process.GetProcessesByName(BusinessSoftware).Length > 0)
			{
				if (!logSentStart)
				{
					logger.Log(new() { Level = Level.Warning, Message = $"[PAUSE] Business software '{BusinessSoftware}' detected. Waiting for closure..." });
					logSentStart = true;
				}

				job.State = State.Paused; // TODO: Que pour la sauvegarde différentielle?
				progressCallback.Invoke(new()
				{
					BackupName     = job.Name,
					State          = State.Paused,
					Message        = $"Waiting for: {BusinessSoftware} closure...",
					TotalFiles     = _totalFiles,
					TotalSize      = _totalSize,
					FilesRemaining = _totalFiles,
					SizeRemaining  = _totalSize
				});

				Thread.Sleep(2000);

				if (job.Cts.IsCancellationRequested)
				{
					if (priorityFiles.Count > 0)
						Interlocked.Add(ref BackupManager.GlobalPriorityFilesPending, -priorityFiles.Count);
					job.State = State.Error;
					return;
				}
			}

			// --- 5. Backup Loop ---
			ExecuteCoreJob(job, BusinessSoftware, progressCallback);

			// --- 6. Final State ---
			if (job.State != State.Error)
			{
				progressCallback.Invoke(new()
				{
					BackupName         = job.Name,
					State              = State.Completed,
					ProgressPercentage = 100
				});
			}
		}

		public abstract void ExecuteCoreJob(BackupJob job, string BusinessSoftware, Action<ProgressState> progressCallback);

		public static void Encrypt(string targetFile, string cryptoPath, string cryptoKey, ref int encryptionTime)
		{
			var config = BackupManager.GetBM().ConfigManager;
			string ext = Path.GetExtension(targetFile);
			if (File.Exists(cryptoPath) && config.GetConfig<List<string>>("PriorityExtensions").Contains(ext))
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
		}
	}
}