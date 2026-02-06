using EasySave.Backup;
using Newtonsoft.Json;

namespace EasySave.Utils
{
	/// <summary>
	/// Manages application configuration and backup jobs persistence
	/// </summary>
	public class ConfigurationManager
	{
		public readonly string _configDirectory;
		public readonly string _configFilePath;
		public readonly string _savedBackupJobPath;

		public dynamic ConfigValues { get; private set; }

		public ConfigurationManager(string configDirectory)
		{
			_configDirectory = configDirectory;
			
			if (!Directory.Exists(_configDirectory))
			{
				Directory.CreateDirectory(_configDirectory);
			}

			_configFilePath     = Path.Combine(_configDirectory, "config.json");
			_savedBackupJobPath = Path.Combine(_configDirectory, "backups.json");
			if (!File.Exists(_configFilePath))
				File.Create(_configFilePath);
			if (new FileInfo(_configFilePath).Length == 0)
			{
				File.WriteAllText(_configFilePath, ResourceManager.ReadResourceFile("default.json"));
			}
			string jsonContent = File.ReadAllText(_configFilePath);
			ConfigValues = JsonConvert.DeserializeObject(jsonContent);
		}

		public List<BackupJob> LoadBackupJobs()
		{
			if (!File.Exists(_savedBackupJobPath))
			{
				return [];
			}

			try
			{
				string jsonContent = File.ReadAllText(_savedBackupJobPath);
				var jobs = JsonConvert.DeserializeObject<List<BackupJob>>(jsonContent);
				return jobs ?? [];
			}
			catch (Exception)
			{
				return [];
			}
		}

		public void SaveBackupJobs(List<BackupJob> jobs)
		{
			try
			{
				string jsonContent = JsonConvert.SerializeObject(jobs, Formatting.Indented);
				File.WriteAllText(_savedBackupJobPath, jsonContent);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to save configuration: {ex.Message}");
			}
		}
	}
}
