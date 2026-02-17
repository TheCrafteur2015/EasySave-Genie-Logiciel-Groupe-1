using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;

namespace EasySave.Backup
{
    /// <summary>
    /// Backup Manager - Central orchestrator for managing backup operations.
    /// Implements the Singleton pattern and acts as the ViewModel in the MVVM architecture.
    /// </summary>
    public class BackupManager
    {
        /// <summary>
        /// Global system Mutex used to ensure that CryptoSoft remains a single-instance process across the system.
        /// </summary>
        public static readonly Mutex CryptoSoftMutex = new Mutex(false, @"Global\EasySave_CryptoSoft_Lock");

        /// <summary>
        /// Volatile counter tracking the number of priority files currently waiting or being processed.
        /// </summary>
        public static volatile int GlobalPriorityFilesPending = 0;

        /// <summary>
        /// Semaphore used to limit the concurrent transfer of large files to prevent bandwidth saturation.
        /// </summary>
        public static SemaphoreSlim BigFileSemaphore = new(1, 1);

        private static BackupManager? _instance;
        private static ILogger? _logger;
        private static readonly object _lock = new();

        private readonly List<BackupJob> _backupJobs;

        /// <summary>
        /// Gets the configuration manager responsible for application settings.
        /// </summary>
        public readonly ConfigurationManager ConfigManager;

        private readonly StateWriter _stateWriter;

        /// <summary>
        /// Gets the maximum number of allowed backup jobs. Returns -1 if no limit is applied.
        /// </summary>
        public readonly int MaxBackupJobs;

        private readonly string appData;

        /// <summary>
        /// Gets the latest control signal received by the manager.
        /// </summary>
        public Signal LatestSignal { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BackupManager class.
        /// Private constructor to enforce the Singleton pattern and initialize application paths and components.
        /// </summary>
        private BackupManager()
        {
            // Initialize paths
            appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave"
            );

            // Initialize components
            _stateWriter = new StateWriter(Path.Combine(appData, "State"));
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
        /// Retrieves the unique instance of the BackupManager (Singleton).
        /// </summary>
        /// <returns>The Singleton instance of the BackupManager.</returns>
        public static BackupManager GetBM()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new BackupManager();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Retrieves the global logger instance configured according to the application settings.
        /// </summary>
        /// <returns>An instance of a class implementing ILogger.</returns>
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
        /// Gets a list of all currently configured backup jobs.
        /// </summary>
        /// <returns>A list containing all BackupJob objects.</returns>
        public List<BackupJob> GetAllJobs() => [.. _backupJobs];

        /// <summary>
        /// Adds a new backup job to the manager and persists it to configuration.
        /// </summary>
        /// <param name="name">The name of the job.</param>
        /// <param name="sourceDir">The source directory path.</param>
        /// <param name="targetDir">The target directory path.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <returns>True if the job was added successfully; otherwise, false (e.g., if the limit is reached).</returns>
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

            int newId = _backupJobs.Count != 0 ? _backupJobs.Max(j => j.Id) + 1 : 1;

            var job = new BackupJob(newId, name, sourceDir, targetDir, type);
            _backupJobs.Add(job);
            ConfigManager.SaveBackupJobs(_backupJobs);

            return true;
        }

        /// <summary>
        /// Deletes a specific backup job by its identifier.
        /// </summary>
        /// <param name="id">The ID of the job to delete.</param>
        /// <returns>True if the job was found and deleted; otherwise, false.</returns>
        public bool DeleteJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return false;

            _backupJobs.Remove(job);
            ConfigManager.SaveBackupJobs(_backupJobs);
            return true;
        }

        /// <summary>
        /// Executes a single backup job synchronously.
        /// </summary>
        /// <param name="id">The ID of the job to execute.</param>
        /// <param name="progressCallback">Optional callback to receive progress updates.</param>
        /// <returns>True if the job completed without errors; otherwise, false.</returns>
        public bool ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) throw new ArgumentException($"Backup job with ID {id} not found.");

            ExecuteSingleJob(job, progressCallback);
            return job.State != State.Error;
        }

        /// <summary>
        /// Executes a range of backup jobs sequentially.
        /// </summary>
        /// <param name="startId">The starting ID of the range.</param>
        /// <param name="endId">The ending ID of the range.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
        {
            List<Task> tasks = new();
            for (int i = startId; i <= endId; i++)
            {
                var job = _backupJobs.FirstOrDefault(j => j.Id == i);
                if (job != null) tasks.Add(Task.Run(() => ExecuteSingleJob(job, progressCallback)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Executes a specific list of backup jobs sequentially.
        /// </summary>
        /// <param name="ids">Array of job IDs to execute.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
        {
            List<Task> tasks = new();
            foreach (var id in ids)
            {
                var job = _backupJobs.FirstOrDefault(j => j.Id == id);
                if (job != null) tasks.Add(Task.Run(() => ExecuteSingleJob(job, progressCallback)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Triggers the asynchronous execution of all configured backup jobs.
        /// </summary>
        /// <param name="progressCallback">Optional callback for real-time progress updates.</param>
        /// <returns>A list of running Tasks representing the background execution of the jobs.</returns>
        public List<Task> ExecuteAllJobsAsync(Action<ProgressState>? progressCallback = null)
        {
            List<Task> tasks = new();
            foreach (var job in _backupJobs)
            {
                job.ResetControls();
                tasks.Add(Task.Run(() => ExecuteSingleJob(job, progressCallback)));
            }
            return tasks;
        }

        /// <summary>
        /// Triggers the asynchronous execution of a single backup job.
        /// </summary>
        /// <param name="id">The ID of the job to execute.</param>
        /// <param name="progressCallback">Optional callback for real-time progress updates.</param>
        /// <returns>A Task representing the background execution of the job.</returns>
        public Task ExecuteJobAsync(int id, Action<ProgressState>? progressCallback = null)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) throw new ArgumentException($"Backup job {id} not found.");

            job.ResetControls();
            return Task.Run(() => ExecuteSingleJob(job, progressCallback));
        }

        /// <summary>
        /// Pauses a specific backup job.
        /// </summary>
        /// <param name="id">The ID of the job to pause.</param>
        public void PauseJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.PauseWaitHandle.Reset();
        }

        /// <summary>
        /// Resumes a previously paused backup job.
        /// </summary>
        /// <param name="id">The ID of the job to resume.</param>
        public void ResumeJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.PauseWaitHandle.Set();
        }

        /// <summary>
        /// Stops the execution of a specific backup job immediately.
        /// </summary>
        /// <param name="id">The ID of the job to stop.</param>
        public void StopJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.Cts.Cancel();
        }

        /// <summary>
        /// Stops the execution of all currently running backup jobs.
        /// </summary>
        public void StopAllJobs()
        {
            foreach (var job in _backupJobs) job.Cts.Cancel();
        }

        /// <summary>
        /// Pauses all currently running backup jobs.
        /// </summary>
        public void PauseAllJobs()
        {
            foreach (var job in _backupJobs) job.PauseWaitHandle.Reset();
        }

        /// <summary>
        /// Resumes all currently paused backup jobs.
        /// </summary>
        public void ResumeAllJobs()
        {
            foreach (var job in _backupJobs) job.PauseWaitHandle.Set();
        }

        /// <summary>
        /// Internal method to execute a single backup job and manage its state and logging.
        /// </summary>
        /// <remarks>
        /// This method wraps the job execution in a try-catch block to ensure errors are logged
        /// and real-time state files are cleaned up correctly in the 'finally' block.
        /// </remarks>
        /// <param name="job">The backup job to execute.</param>
        /// <param name="progressCallback">Callback to propagate progress to the UI.</param>
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
            }
            finally
            {
                _stateWriter.RemoveState(job.Name);
            }
        }

        /// <summary>
        /// Transmits a control signal to the manager to influence the application flow.
        /// </summary>
        /// <param name="signal">The signal to transmit (e.g., Exit, Continue).</param>
        public void TransmitSignal(Signal signal)
        {
            LatestSignal = signal;
        }
    }
}