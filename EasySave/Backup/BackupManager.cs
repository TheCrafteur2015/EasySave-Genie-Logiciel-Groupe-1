using EasyLog.Logging;
using EasySave.Utils;
using Newtonsoft.Json.Linq;

namespace EasySave.Backup
{
	/// <summary>
	/// Backup Manager - Orchestrateur central des opérations de sauvegarde.
	/// Implémente le pattern Singleton et sert de ViewModel dans l'architecture MVVM.
	/// </summary>
	public class BackupManager
	{
		// --- SYNCHRONISATION GLOBALE ---

		/// <summary>
		/// Mutex global pour garantir que CryptoSoft est une instance unique sur tout le système.
		/// </summary>
		public static readonly Mutex CryptoSoftMutex = new Mutex(false, @"Global\EasySave_CryptoSoft_Lock");

		/// <summary>
		/// Compteur volatile pour suivre le nombre de fichiers prioritaires en attente dans tous les jobs.
		/// </summary>
		public static volatile int GlobalPriorityFilesPending = 0;

		/// <summary>
		/// Sémaphore pour limiter le transfert simultané de gros fichiers (évite la saturation bande passante).
		/// </summary>
		public static SemaphoreSlim BigFileSemaphore = new(1, 1);

		// --- SINGLETON & ETAT ---
		private static BackupManager? _instance;
		private static ILogger? _logger;
		private static readonly object _lock = new();

		private readonly List<BackupJob> _backupJobs;
		public readonly ConfigurationManager ConfigManager;
		private readonly StateWriter _stateWriter;
		public readonly int MaxBackupJobs;
		private readonly string appData;

		public Signal LatestSignal { get; private set; }

		private BackupManager()
		{
			// Initialisation des chemins
			appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");

			// Initialisation des composants
			_stateWriter = new StateWriter(Path.Combine(appData, "State"));
			ConfigManager = new ConfigurationManager(Path.Combine(appData, "Config"));

			// Gestion de la limite de jobs
			MaxBackupJobs = ConfigManager.GetConfig("MaxBackupJobs");
			var useBackupJobLimit = ConfigManager.GetConfig("UseBackupJobLimit") as JValue;
			if (useBackupJobLimit?.Value is bool val && val == false) MaxBackupJobs = -1;

			// Chargement des travaux
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

		// --- GESTION DES JOBS (CRUD) ---

		public List<BackupJob> GetAllJobs() => [.. _backupJobs];

		public bool AddJob(string? name, string? sourceDir, string? targetDir, BackupType type)
		{
			if (_backupJobs.Count >= MaxBackupJobs && MaxBackupJobs != -1) return false;
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(targetDir)) return false;

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

		// --- EXÉCUTION & CONTRÔLE (ASYNCHRONE) ---

		/// <summary>
		/// Exécute un job unique de manière asynchrone (utile pour le monitoring).
		/// </summary>
		public Task ExecuteJobAsync(int id, Action<ProgressState>? progressCallback = null)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null) throw new ArgumentException($"Job {id} not found.");
			
			job.ResetControls(); // Reset Pause/Stop tokens
			return Task.Run(() => ExecuteSingleJob(job, progressCallback));
		}

		public void ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
		{
			ExecuteJobAsync(id, progressCallback).Wait();
		}

		public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
		{
			var tasks = _backupJobs.Where(j => j.Id >= startId && j.Id <= endId)
								   .Select(j => { j.ResetControls(); return Task.Run(() => ExecuteSingleJob(j, progressCallback)); });
			Task.WaitAll(tasks.ToArray());
		}

		public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
		{
			var tasks = _backupJobs.Where(j => ids.Contains(j.Id))
								   .Select(j => { j.ResetControls(); return Task.Run(() => ExecuteSingleJob(j, progressCallback)); });
			Task.WaitAll(tasks.ToArray());
		}

		/// <summary>
		/// Déclenche l'exécution de tous les jobs et retourne la liste des tâches pour le moniteur.
		/// </summary>
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

		public void ExecuteAllJobs(Action<ProgressState>? progressCallback = null)
		{
			Task.WaitAll(ExecuteAllJobsAsync(progressCallback).ToArray());
		}

		// --- MÉTHODES DE PILOTAGE ---

		public void PauseJob(int id) => _backupJobs.FirstOrDefault(j => j.Id == id)?.PauseWaitHandle.Reset();
		public void ResumeJob(int id) => _backupJobs.FirstOrDefault(j => j.Id == id)?.PauseWaitHandle.Set();
		public void StopJob(int id) => _backupJobs.FirstOrDefault(j => j.Id == id)?.Cts.Cancel();

		public void PauseAllJobs() => _backupJobs.ForEach(j => j.PauseWaitHandle.Reset());
		public void ResumeAllJobs() => _backupJobs.ForEach(j => j.PauseWaitHandle.Set());
		public void StopAllJobs() => _backupJobs.ForEach(j => j.Cts.Cancel());

		// --- LOGIQUE INTERNE ---

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

		public void TransmitSignal(Signal signal) => LatestSignal = signal;
	}
}