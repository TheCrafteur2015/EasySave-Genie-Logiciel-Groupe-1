using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;

namespace EasySave.Backup
{
	/// <summary>
	/// Backup Manager - Singleton pattern for managing backup operations
	/// Acts as the ViewModel in MVVM architecture
	/// </summary>
	public class BackupManager
	{
		private static BackupManager? _instance;
		private static ILogger? _logger;
		private static readonly object _lock = new();
		
		private readonly List<BackupJob> _backupJobs;
		public readonly ConfigurationManager ConfigManager;
		private readonly StateWriter _stateWriter;

		public readonly int MaxBackupJobs;
		private readonly string appData;


		public Signal LatestSignal { get; private set; }
    
    /// <summary>
		/// Initializes a new instance of the BackupManager class and sets up required components and configuration.
		/// </summary>
		/// <remarks>This constructor is private and is intended to restrict instantiation of the BackupManager class
		/// to within the class itself, typically to implement a singleton or controlled creation pattern.</remarks>
		private BackupManager()
		{
			// Initialize paths
			appData = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"EasySave"
			);

			// Initialize components
			_stateWriter  = new StateWriter(Path.Combine(appData, "State"));
			ConfigManager = new ConfigurationManager(Path.Combine(appData, "Config"));

			MaxBackupJobs = ConfigManager.GetConfig("MaxBackupJobs");
			var useBackupJobLimit = ConfigManager.GetConfig("UseBackupJobLimit") as JValue;
			if (useBackupJobLimit?.Value is bool val && val == false)
				MaxBackupJobs = -1;

			// Load existing jobs
			_backupJobs = ConfigManager.LoadBackupJobs();

			LatestSignal = Signal.None;
		}

		/// <summary>
		/// Retrieves the singleton instance of the BackupManager.
		/// </summary>
		/// <remarks>This method ensures that only one instance of BackupManager exists throughout the application's
		/// lifetime. The instance is created on first access and is thread-safe.</remarks>
		/// <returns>The single instance of the BackupManager used by the application.</returns>
		public static BackupManager GetBM()
		{
			if (_instance == null)
			{
				lock (_lock)
				{
					_instance = new BackupManager();
				}
			}
			return _instance;
		}
		
		public static ILogger GetLogger()
		{
			if (_logger == null)
			{
				var BM = GetBM();
				var format = BM.ConfigManager.GetConfig("LoggerFormat");
				lock (_lock)
				{
					_logger = LoggerFactory.CreateLogger(format?.Value as string ?? "text", Path.Combine(BM.appData, "Logs"));
				}
			}
			return _logger;
		}
		
		/// <summary>
		/// Retrieves a list of all configured backup jobs.
		/// </summary>
		/// <returns>A list of <see cref="BackupJob"/> objects representing all backup jobs. The list will be empty if no jobs are
		/// configured.</returns>
		public List<BackupJob> GetAllJobs() => [.. _backupJobs];

		/// <summary>
		/// Attempts to add a new backup job with the specified parameters.
		/// </summary>
		/// <remarks>The method will not add a job if the maximum allowed number of backup jobs has already been
		/// reached. Parameter values must be valid and non-empty to successfully add a job.</remarks>
		/// <param name="name">The name of the backup job. Cannot be null, empty, or consist only of white-space characters.</param>
		/// <param name="sourceDir">The source directory to back up. Cannot be null, empty, or consist only of white-space characters.</param>
		/// <param name="targetDir">The target directory where the backup will be stored. Cannot be null, empty, or consist only of white-space
		/// characters.</param>
		/// <param name="type">The type of backup to perform for the job.</param>
		/// <returns>true if the backup job was added successfully; otherwise, false. Returns false if the maximum number of backup
		/// jobs has been reached or if any parameter is invalid.</returns>
		public bool AddJob(string? name, string? sourceDir, string? targetDir, BackupType type)
		{
			if (_backupJobs.Count >= MaxBackupJobs && MaxBackupJobs != -1)
				return false;

			if (string.IsNullOrWhiteSpace(name) ||
				string.IsNullOrWhiteSpace(sourceDir) ||
				string.IsNullOrWhiteSpace(targetDir))
			{
				return false;
			}

			if (string.IsNullOrEmpty(name) ||
				string.IsNullOrEmpty(sourceDir) ||
				string.IsNullOrEmpty(targetDir))
			{
				return false;
			}

			int newId = _backupJobs.Count != 0 ? _backupJobs.Max(j => j.Id) + 1 : 1;

			var job = new BackupJob(newId, name, sourceDir, targetDir, type);
			_backupJobs.Add(job);
			ConfigManager.SaveBackupJobs(_backupJobs);

			return true;
		}

		/// <summary>
		/// Deletes the backup job with the specified identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the backup job to delete.</param>
		/// <returns>true if the backup job was found and deleted; otherwise, false.</returns>
		public bool DeleteJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null)
				return false;

			_backupJobs.Remove(job);
			ConfigManager.SaveBackupJobs(_backupJobs);
			return true;
		}

		/// <summary>
		/// Executes the backup job with the specified identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the backup job to execute.</param>
		/// <param name="progressCallback">An optional callback that receives progress updates during job execution. If null, progress updates are not
		/// reported.</param>
		/// <exception cref="ArgumentException">Thrown if a backup job with the specified ID does not exist.</exception>
		public bool ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null)
				throw new ArgumentException($"Backup job with ID {id} not found.");

			ExecuteSingleJob(job, progressCallback);

            return job.State != State.Error;
        }

		/// <summary>
		/// Executes all backup jobs with IDs in the specified inclusive range, optionally reporting progress for each job.
		/// </summary>
		/// <param name="startId">The first job ID in the range to execute. Must be less than or equal to <paramref name="endId"/>.</param>
		/// <param name="endId">The last job ID in the range to execute. Must be greater than or equal to <paramref name="startId"/>.</param>
		/// <param name="progressCallback">An optional callback that receives progress updates for each job as it is executed. If <see langword="null"/>, no
		/// progress is reported.</param>
		public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
		{
			for (int i = startId; i <= endId; i++)
			{
				var job = _backupJobs.FirstOrDefault(j => j.Id == i);
				if (job != null)
					ExecuteSingleJob(job, progressCallback);
			}
		}

		/// <summary>
		/// Executes the backup jobs corresponding to the specified job IDs, optionally reporting progress for each job.
		/// </summary>
		/// <param name="ids">An array of job identifiers specifying which backup jobs to execute. Only jobs with matching IDs will be
		/// processed.</param>
		/// <param name="progressCallback">An optional callback that receives progress updates for each job as it executes. If null, progress is not
		/// reported.</param>
		public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
		{
			foreach (var id in ids)
			{
				var job = _backupJobs.FirstOrDefault(j => j.Id == id);
				if (job != null)
					ExecuteSingleJob(job, progressCallback);
			}
		}

		/// <summary>
		/// Executes all configured backup jobs in sequence, optionally reporting progress for each job.
		/// </summary>
		/// <remarks>Each backup job is executed in the order in which it was added. The method blocks until all jobs
		/// have completed. If a progress callback is provided, it is invoked for each job's progress updates.</remarks>
		/// <param name="progressCallback">An optional callback that receives progress updates for each job as a <see cref="ProgressState"/> instance. If
		/// <see langword="null"/>, no progress is reported.</param>
		public void ExecuteAllJobs(Action<ProgressState>? progressCallback = null)
		{
			foreach (var job in _backupJobs)
			{
				ExecuteSingleJob(job, progressCallback);
			}
		}

		/// <summary>
		/// Executes the specified backup job and updates progress using the provided callback.
		/// </summary>
		/// <remarks>If an exception occurs during job execution, the job is marked as failed and the error is logged
		/// before the exception is rethrown. The job's progress state is removed after execution completes, regardless of
		/// success or failure.</remarks>
		/// <param name="job">The backup job to execute. Cannot be null.</param>
		/// <param name="progressCallback">An optional callback that receives progress updates as the job executes. If null, progress updates are not
		/// reported to the caller.</param>
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
				BackupManager.GetLogger().LogError(e);
				throw;
			}
			finally
			{
				_stateWriter.RemoveState(job.Name);
			}
		}

		public void TransmitSignal(Signal signal)
		{
			LatestSignal = signal;
		}

	}
}
