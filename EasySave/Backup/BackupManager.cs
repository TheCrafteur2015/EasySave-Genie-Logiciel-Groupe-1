using EasyLog.Logging;
using EasySave.Utils;

namespace EasySave.Backup
{
	/// <summary>
	/// Central orchestrator for backup operations.
	/// Implements the Singleton pattern and serves as the core engine for the application.
	/// </summary>
	public class BackupManager
	{
		// --- GLOBAL SYNCHRONIZATION ---

		/// <summary>
		/// Global mutex used to ensure that the CryptoSoft application remains a single instance across the entire system.
		/// </summary>
		public static readonly Mutex CryptoSoftMutex = new(false, @"Global\EasySave_CryptoSoft_Lock");

		/// <summary>
		/// Volatile counter to track the number of priority files currently pending across all active backup jobs.
		/// </summary>
		public static volatile int GlobalPriorityFilesPending = 0;

		/// <summary>
		/// Semaphore used to limit the simultaneous transfer of large files to prevent bandwidth saturation.
		/// </summary>
		public static SemaphoreSlim BigFileSemaphore = new(1, 1);

		// --- SINGLETON & STATE ---
		private static BackupManager? _instance;
		private static ILogger? _logger;
		private static readonly object _lock = new();

		private readonly List<BackupJob> _backupJobs;
		public readonly ConfigurationManager ConfigManager;
		private readonly StateWriter _stateWriter;
		public readonly int MaxBackupJobs;
		private readonly string appData;

		/// <summary>
		/// Gets the latest signal received by the manager.
		/// </summary>
		public Signal LatestSignal { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BackupManager"/> class.
		/// Sets up application paths, initializes managers, and loads existing backup jobs.
		/// </summary>
		private BackupManager()
		{
			AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
				ConfigManager?.SaveConfiguration();
			};

			// Initialize paths
			appData = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"EasySave"
			);

			// Component initialization
			_stateWriter = new StateWriter(Path.Combine(appData, "State"));
			ConfigManager = new ConfigurationManager(Path.Combine(appData, "Config"));

			MaxBackupJobs = ConfigManager.GetConfig<int>("MaxBackupJobs");
			var useBackupJobLimit = ConfigManager.GetConfig<bool>("UseBackupJobLimit");
			if (!useBackupJobLimit)
				MaxBackupJobs = -1;

			// Load backup jobs
			_backupJobs = ConfigManager.LoadBackupJobs();
			LatestSignal = Signal.None;
		}

		/// <summary>
		/// Returns the singleton instance of the <see cref="BackupManager"/>.
		/// </summary>
		/// <returns>The unique instance of the manager.</returns>
		public static BackupManager GetBM()
		{
			lock (_lock)
			{
				_instance ??= new BackupManager();
			}
			return _instance;
		}

		/// <summary>
		/// Returns the singleton instance of the logger, initialized with the format from configuration.
		/// </summary>
		/// <returns>An implementation of the <see cref="ILogger"/> interface.</returns>
		public static ILogger GetLogger()
		{
			lock (_lock)
			{
				if (_logger == null)
				{
					var BM = GetBM();
					var format = BM.ConfigManager.GetConfig<string>("LoggerFormat");
					_logger = LoggerFactory.CreateLogger(format ?? "text", Path.Combine(BM.appData, "Logs"));
				}
			}
			return _logger;
		}

		// --- JOB MANAGEMENT (CRUD) ---

		/// <summary>
		/// Retrieves a copy of all configured backup jobs.
		/// </summary>
		/// <returns>A list of backup jobs.</returns>
		public List<BackupJob> GetAllJobs() => [.. _backupJobs];

		/// <summary>
		/// Adds a new backup job to the list and saves the updated configuration.
		/// </summary>
		/// <param name="name">The display name of the job.</param>
		/// <param name="sourceDir">The source directory path.</param>
		/// <param name="targetDir">The destination directory path.</param>
		/// <param name="type">The type of backup (Full or Differential).</param>
		/// <returns>True if the job was successfully added, otherwise false.</returns>
		public bool AddJob(string? name, string? sourceDir, string? targetDir, BackupType type)
		{
			if (_backupJobs.Count >= MaxBackupJobs && MaxBackupJobs != -1) return false;
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(targetDir)) return false;
			if (_backupJobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;

			int newId = _backupJobs.Count != 0 ? _backupJobs.Max(j => j.Id) + 1 : 1;
			var job = new BackupJob(newId, name, sourceDir, targetDir, type);
			_backupJobs.Add(job);
			ConfigManager.SaveBackupJobs(_backupJobs);
			return true;
		}

		/// <summary>
		/// Deletes a backup job by its ID and re-indexes the remaining jobs.
		/// </summary>
		/// <param name="id">The unique identifier of the job to delete.</param>
		/// <returns>True if the job was found and deleted, otherwise false.</returns>
		public bool DeleteJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null) return false;
			_backupJobs.Remove(job);
			for (int i = 0; i < _backupJobs.Count; i++)
			{
				_backupJobs[i].Id = i + 1;
			}
			ConfigManager.SaveBackupJobs(_backupJobs);
			return true;
		}

		// --- EXECUTION & CONTROL (ASYNCHRONOUS) ---

		/// <summary>
		/// Executes a single backup job asynchronously.
		/// </summary>
		/// <param name="id">The ID of the job to execute.</param>
		/// <param name="progressCallback">Optional action to handle real-time progress updates.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task ExecuteJobAsync(int id, Action<ProgressState>? progressCallback = null)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id) ?? throw new ArgumentException($"Job {id} not found.");
			job.ResetControls(); // Reset Pause/Stop tokens
			return Task.Run(() => ExecuteSingleJob(job, progressCallback));
		}

		/// <summary>
		/// Executes a single backup job and waits for its completion.
		/// </summary>
		/// <param name="id">The ID of the job to execute.</param>
		/// <param name="progressCallback">Optional progress update handler.</param>
		public void ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
		{
			ExecuteJobAsync(id, progressCallback).Wait();
		}

		/// <summary>
		/// Executes a range of backup jobs simultaneously based on their IDs.
		/// </summary>
		/// <param name="startId">The starting ID of the range.</param>
		/// <param name="endId">The ending ID of the range.</param>
		/// <param name="progressCallback">Optional progress update handler.</param>
		public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
		{
			var tasks = _backupJobs.Where(j => j.Id >= startId && j.Id <= endId)
								   .Select(j => { j.ResetControls(); return Task.Run(() => ExecuteSingleJob(j, progressCallback)); });
			Task.WaitAll([.. tasks]);
		}

		/// <summary>
		/// Executes a specific list of backup jobs based on the provided IDs.
		/// </summary>
		/// <param name="ids">An array of job IDs to execute.</param>
		/// <param name="progressCallback">Optional progress update handler.</param>
		public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
		{
			var tasks = _backupJobs.Where(j => ids.Contains(j.Id))
								   .Select(j => { j.ResetControls(); return Task.Run(() => ExecuteSingleJob(j, progressCallback)); });
			Task.WaitAll([.. tasks]);
		}

		/// <summary>
		/// Initiates the asynchronous execution of all configured backup jobs.
		/// </summary>
		/// <param name="progressCallback">Optional progress update handler.</param>
		/// <returns>A list of tasks representing the running backup operations.</returns>
		public List<Task> ExecuteAllJobsAsync(Action<ProgressState>? progressCallback = null)
		{
			List<Task> tasks = [];
			foreach (var job in _backupJobs)
			{
				job.ResetControls();
				tasks.Add(Task.Run(() => ExecuteSingleJob(job, progressCallback)));
			}
			return tasks;
		}

		/// <summary>
		/// Executes all configured backup jobs and waits for all of them to complete.
		/// </summary>
		/// <param name="progressCallback">Optional progress update handler.</param>
		public void ExecuteAllJobs(Action<ProgressState>? progressCallback = null)
		{
			Task.WaitAll([.. ExecuteAllJobsAsync(progressCallback)]);
		}

		// --- STEERING METHODS ---

		/// <summary>
		/// Stops a specific running job by canceling its token and releasing its wait handle.
		/// </summary>
		/// <param name="id">The ID of the job to stop.</param>
		public void StopJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job != null)
			{
				job.Cts.Cancel();
				job.PauseWaitHandle.Set(); // Resume thread if it was paused to allow immediate exit
			}
		}

		/// <summary>
		/// Stops all currently running backup jobs.
		/// </summary>
		public void StopAllJobs()
		{
			foreach (var job in _backupJobs)
			{
				job.Cts.Cancel();
				job.PauseWaitHandle.Set();
			}
		}

		/// <summary>
		/// Pauses a specific running job by resetting its wait handle.
		/// </summary>
		/// <param name="id">The ID of the job to pause.</param>
		public void PauseJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job != null)
			{
				job.State = State.Paused;
				job.PauseWaitHandle.Reset();
			}
		}

		/// <summary>
		/// Resumes a specific paused job by signaling its wait handle.
		/// </summary>
		/// <param name="id">The ID of the job to resume.</param>
		public void ResumeJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job != null)
			{
				job.State = State.Active;
				job.PauseWaitHandle.Set();
			}
		}

		/// <summary>
		/// Pauses all currently running backup jobs.
		/// </summary>
		public void PauseAllJobs()
		{
			foreach (var job in _backupJobs)
			{
				job.State = State.Paused;
				job.PauseWaitHandle.Reset();
			}
		}

		/// <summary>
		/// Resumes all currently paused backup jobs.
		/// </summary>
		public void ResumeAllJobs()
		{
			foreach (var job in _backupJobs)
			{
				job.State = State.Active;
				job.PauseWaitHandle.Set();
			}
		}

		// --- INTERNAL LOGIC ---

		/// <summary>
		/// Internal method to handle the execution flow of a single job, including state persistence and logging.
		/// </summary>
		private void ExecuteSingleJob(BackupJob job, Action<ProgressState>? progressCallback)
		{
			try
			{
				void ProgressHandler(ProgressState state)
				{
					_stateWriter.UpdateState(state);
					progressCallback?.Invoke(state);
				}
				job.Execute(ProgressHandler);
				ConfigManager.SaveBackupJobs(_backupJobs);
			}
			catch (Exception e)
			{
				job.Error();
				GetLogger().LogError(e);
			}
			finally
			{
				_stateWriter.RemoveState(job.Name);
			}
		}

		/// <summary>
		/// Updates the latest signal stored in the manager.
		/// </summary>
		/// <param name="signal">The signal to transmit.</param>
		public void TransmitSignal(Signal signal) => LatestSignal = signal;
	}
}