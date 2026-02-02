using EasySave.Models;
using EasySave.Strategies;
using EasySave.Utils;
using EasySave.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.ViewModels
{
    /// <summary>
    /// Backup Manager - Singleton pattern for managing backup operations
    /// Acts as the ViewModel in MVVM architecture
    /// </summary>
    public class BackupManager
    {
        private static BackupManager? _instance;
        private static readonly object _lock = new object();

        private readonly List<BackupJob> _backupJobs;
        private readonly ConfigurationManager _configManager;
        private readonly StateWriter _stateWriter;
        private readonly ILogger _logger;
        private readonly BackupStrategyFactory _strategyFactory;

        private const int MaxBackupJobs = 5;

        private BackupManager()
        {
            // Initialize paths
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave"
            );

            string logPath = Path.Combine(appDataPath, "Logs");
            string statePath = Path.Combine(appDataPath, "State");
            string configPath = Path.Combine(appDataPath, "Config");

            // Initialize components
            _logger = new JsonLogger(logPath);
            _stateWriter = new StateWriter(statePath);
            _configManager = new ConfigurationManager(configPath);
            _strategyFactory = new BackupStrategyFactory(_logger);

            // Load existing jobs
            _backupJobs = _configManager.LoadBackupJobs();
        }

        public static BackupManager GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new BackupManager();
                    }
                }
            }
            return _instance;
        }

        public List<BackupJob> GetAllJobs()
        {
            return new List<BackupJob>(_backupJobs);
        }

        public bool AddJob(string name, string sourceDir, string targetDir, BackupType type)
        {
            if (_backupJobs.Count >= MaxBackupJobs)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(name) || 
                string.IsNullOrWhiteSpace(sourceDir) || 
                string.IsNullOrWhiteSpace(targetDir))
            {
                return false;
            }

            int newId = _backupJobs.Any() ? _backupJobs.Max(j => j.Id) + 1 : 1;

            var job = new BackupJob(newId, name, sourceDir, targetDir, type);
            _backupJobs.Add(job);
            _configManager.SaveBackupJobs(_backupJobs);

            return true;
        }

        public bool DeleteJob(int id)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                return false;
            }

            _backupJobs.Remove(job);
            _configManager.SaveBackupJobs(_backupJobs);
            return true;
        }

        public void ExecuteJob(int id, Action<ProgressState>? progressCallback = null)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                throw new ArgumentException($"Backup job with ID {id} not found.");
            }

            ExecuteSingleJob(job, progressCallback);
        }

        public void ExecuteJobRange(int startId, int endId, Action<ProgressState>? progressCallback = null)
        {
            for (int i = startId; i <= endId; i++)
            {
                var job = _backupJobs.FirstOrDefault(j => j.Id == i);
                if (job != null)
                {
                    ExecuteSingleJob(job, progressCallback);
                }
            }
        }

        public void ExecuteJobList(int[] ids, Action<ProgressState>? progressCallback = null)
        {
            foreach (var id in ids)
            {
                var job = _backupJobs.FirstOrDefault(j => j.Id == id);
                if (job != null)
                {
                    ExecuteSingleJob(job, progressCallback);
                }
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
                job.State = BackupState.Active;
                job.Strategy = _strategyFactory.CreateStrategy(job.Type);

                void ProgressHandler(ProgressState state)
                {
                    _stateWriter.UpdateState(state);
                    progressCallback?.Invoke(state);
                }

                job.Strategy.Execute(job, ProgressHandler);

                job.LastExecution = DateTime.Now;
                job.State = BackupState.Completed;
                _configManager.SaveBackupJobs(_backupJobs);
            }
            catch (Exception ex)
            {
                job.State = BackupState.Error;
                _logger.LogError(job.Name, ex.Message);
                throw;
            }
            finally
            {
                _stateWriter.RemoveState(job.Name);
            }
        }
    }
}
