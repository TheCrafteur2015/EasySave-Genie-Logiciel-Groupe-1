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
        // --- AJOUT INDISPENSABLE POUR LA V3.0 (Mono-Instance CryptoSoft) ---
        public static readonly Mutex CryptoSoftMutex = new Mutex(false, "Global\\EasySave_CryptoSoft_Lock");
        // -------------------------------------------------------------------

        public static volatile int GlobalPriorityFilesPending = 0;
        public static SemaphoreSlim BigFileSemaphore = new(1, 1);
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
        /// Initializes a new instance of the BackupManager class.
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

        public List<BackupJob> GetAllJobs() => [.. _backupJobs];

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

        public bool DeleteJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return false;

            _backupJobs.Remove(job);
            ConfigManager.SaveBackupJobs(_backupJobs);
            return true;
        }

        public bool ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) throw new ArgumentException($"Backup job with ID {id} not found.");

            ExecuteSingleJob(job, progressCallback);
            return job.State != State.Error;
        }

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

        public Task ExecuteJobAsync(int id, Action<ProgressState>? progressCallback = null)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null) throw new ArgumentException($"Backup job {id} not found.");

            job.ResetControls();
            return Task.Run(() => ExecuteSingleJob(job, progressCallback));
        }

        public void PauseJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.PauseWaitHandle.Reset();
        }

        public void ResumeJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.PauseWaitHandle.Set();
        }

        public void StopJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            job?.Cts.Cancel();
        }

        public void StopAllJobs()
        {
            foreach (var job in _backupJobs) job.Cts.Cancel();
        }

        public void PauseAllJobs()
        {
            foreach (var job in _backupJobs) job.PauseWaitHandle.Reset();
        }

        public void ResumeAllJobs()
        {
            foreach (var job in _backupJobs) job.PauseWaitHandle.Set();
        }

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

        public void TransmitSignal(Signal signal)
        {
            LatestSignal = signal;
        }
    }
}