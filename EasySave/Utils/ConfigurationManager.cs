using EasySave.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave.Utils
{
    /// <summary>
    /// Manages application configuration and backup jobs persistence
    /// </summary>
    public class ConfigurationManager
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;

        public ConfigurationManager(string configDirectory)
        {
            _configDirectory = configDirectory;
            
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            _configFilePath = Path.Combine(_configDirectory, "config.json");
        }

        public List<BackupJob> LoadBackupJobs()
        {
            if (!File.Exists(_configFilePath))
            {
                return new List<BackupJob>();
            }

            try
            {
                string jsonContent = File.ReadAllText(_configFilePath);
                var jobs = JsonConvert.DeserializeObject<List<BackupJob>>(jsonContent);
                return jobs ?? new List<BackupJob>();
            }
            catch (Exception)
            {
                return new List<BackupJob>();
            }
        }

        public void SaveBackupJobs(List<BackupJob> jobs)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(jobs, Formatting.Indented);
                File.WriteAllText(_configFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save configuration: {ex.Message}");
            }
        }
    }
}
