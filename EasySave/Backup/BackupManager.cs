using EasyLog.Logging;
using EasySave.Utils;

namespace EasySave.Backup
{
	/// <summary>
	/// Backup Manager - Orchestrateur central des op�rations de sauvegarde.
	/// Impl�mente le pattern Singleton et sert de ViewModel dans l'architecture MVVM.
	/// </summary>
	public class BackupManager
	{
		// --- SYNCHRONISATION GLOBALE ---

		/// <summary>
		/// Mutex global pour garantir que CryptoSoft est une instance unique sur tout le syst�me.
		/// </summary>
		public static readonly Mutex CryptoSoftMutex = new Mutex(false, @"Global\EasySave_CryptoSoft_Lock");

		/// <summary>
		/// Compteur volatile pour suivre le nombre de fichiers prioritaires en attente dans tous les jobs.
		/// </summary>
		public static volatile int GlobalPriorityFilesPending = 0;

		/// <summary>
		/// S�maphore pour limiter le transfert simultan� de gros fichiers (�vite la saturation bande passante).
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
	
		/// <summary>
		/// Initializes a new instance of the BackupManager class and sets up required components and configuration.
		/// </summary>
		/// <remarks>This constructor is private and is intended to restrict instantiation of the BackupManager class
		/// to within the class itself, typically to implement a singleton or controlled creation pattern.</remarks>
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

			// Initialisation des composants
			_stateWriter  = new StateWriter(Path.Combine(appData, "State"));
			ConfigManager = new ConfigurationManager(Path.Combine(appData, "Config"));

			MaxBackupJobs = ConfigManager.GetConfig<int>("MaxBackupJobs");
			var useBackupJobLimit = ConfigManager.GetConfig<bool>("UseBackupJobLimit");
			if (!useBackupJobLimit)
				MaxBackupJobs = -1;

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
				var format = BM.ConfigManager.GetConfig<string>("LoggerFormat");
				lock (_lock)
				{
					_logger = LoggerFactory.CreateLogger(format ?? "text", Path.Combine(BM.appData, "Logs"));
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
            if (_backupJobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;

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
            for (int i = 0; i < _backupJobs.Count; i++)
            {
                _backupJobs[i].Id = i + 1;
            }
            ConfigManager.SaveBackupJobs(_backupJobs);
			return true;
		}

		// --- EX�CUTION & CONTR�LE (ASYNCHRONE) ---

		/// <summary>
		/// Ex�cute un job unique de mani�re asynchrone (utile pour le monitoring).
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
		/// D�clenche l'ex�cution de tous les jobs et retourne la liste des t�ches pour le moniteur.
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

        // --- M�THODES DE PILOTAGE ---

        public void StopJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                job.Cts.Cancel();           // 1. On demande l'arrêt
                job.PauseWaitHandle.Set();  // 2. IMPORTANT : On débloque le thread s'il dormait en pause !
            }
        }

        public void StopAllJobs()
        {
            foreach (var job in _backupJobs)
            {
                job.Cts.Cancel();           // 1. On demande l'arrêt
                job.PauseWaitHandle.Set();  // 2. On réveille tout le monde pour qu'ils s'arrêtent
            }
        }

        // --- MÉTHODES DE PILOTAGE CORRIGÉES ---

        public void PauseJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                job.State = State.Paused; // <--- C'EST CELLE-CI QUI FAIT MARCHER LE BOUTON
                job.PauseWaitHandle.Reset();
            }
        }

        public void ResumeJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                job.State = State.Active; // <--- LIGNE CRUCIALE AJOUTÉE
                job.PauseWaitHandle.Set();
            }
        }

        public void PauseAllJobs()
        {
            foreach (var job in _backupJobs)
            {
                job.State = State.Paused; // <--- FORÇAGE DE L'ÉTAT
                job.PauseWaitHandle.Reset();
            }
        }

        public void ResumeAllJobs()
        {
            foreach (var job in _backupJobs)
            {
                job.State = State.Active; // <--- FORÇAGE DE L'ÉTAT
                job.PauseWaitHandle.Set();
            }
        }
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