using EasySave.Logger;
using EasySave.Models;
using EasySave.Utils;
using EasySave.Views.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

			MaxBackupJobs = ConfigManager.ConfigValues["MaxBackupJobs"];
			_ = new I18n();

			// Load existing jobs
			_backupJobs = ConfigManager.LoadBackupJobs();
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
				lock (_lock)
				{
					_logger = new SimpleLogger(Path.Combine(GetBM().appData, "Logs"));
				}
			}
			return _logger;
		}

		public List<BackupJob> GetAllJobs() => [.. _backupJobs];

		public bool AddJob(string name, string sourceDir, string targetDir, BackupType type)
		{
			if (_backupJobs.Count >= MaxBackupJobs)
				return false;

			if (string.IsNullOrWhiteSpace(name) ||
				string.IsNullOrWhiteSpace(sourceDir) ||
				string.IsNullOrWhiteSpace(targetDir))
			{
				return false;
			}

			int newId = _backupJobs.Any() ? _backupJobs.Max(j => j.Id) + 1 : 1;

			var job = new BackupJob(newId, name, sourceDir, targetDir, type);
			_backupJobs.Add(job);
			ConfigManager.SaveBackupJobs(_backupJobs);

			return true;
		}

		public bool DeleteJob(int id)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null)
				return false;

			_backupJobs.Remove(job);
			ConfigManager.SaveBackupJobs(_backupJobs);
			return true;
		}

		public void ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
		{
			var job = _backupJobs.FirstOrDefault(j => j.Id == id);
			if (job == null)
				throw new ArgumentException($"Backup job with ID {id} not found.");

			ExecuteSingleJob(job, progressCallback);
		}

		public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
		{
			for (int i = startId; i <= endId; i++)
			{
				var job = _backupJobs.FirstOrDefault(j => j.Id == i);
				if (job != null)
					ExecuteSingleJob(job, progressCallback);
			}
		}

		public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
		{
			foreach (var id in ids)
			{
				var job = _backupJobs.FirstOrDefault(j => j.Id == id);
				if (job != null)
					ExecuteSingleJob(job, progressCallback);
			}
		}

		public void ExecuteAllJobs(Action<ProgressState>? progressCallback = null)
		{
			foreach (var job in _backupJobs)
			{
				ExecuteSingleJob(job, progressCallback);
			}
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
				throw;
			}
			finally
			{
				_stateWriter.RemoveState(job.Name);
			}
		}
	}
}
