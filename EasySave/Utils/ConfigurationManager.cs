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

		/// <summary>
		/// Gets the dynamic configuration values for the current instance.
		/// </summary>
		/// <remarks>The returned object provides access to configuration settings whose structure may vary at
		/// runtime. Use dynamic member access to retrieve specific configuration values as needed.</remarks>
		private dynamic ConfigValues { get; set; }

		/// <summary>
		/// Initializes a new instance of the ConfigurationManager class using the specified configuration directory. Ensures
		/// that the configuration files are created and loaded from the given directory.
		/// </summary>
		/// <remarks>If the configuration file does not exist or is empty, a default configuration is loaded. This
		/// constructor ensures that the configuration environment is set up and ready for use.</remarks>
		/// <param name="configDirectory">The path to the directory where configuration files are stored. If the directory does not exist, it will be
		/// created. Cannot be null or empty.</param>
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

		public dynamic GetConfig(string key)
		{
			return ConfigValues[key] ?? throw new ArgumentException("This configuration key doesn't exists!");
		}

		/// <summary>
		/// Loads all saved backup jobs from persistent storage.
		/// </summary>
		/// <remarks>If the backup jobs file does not exist or cannot be read, the method returns an empty list. The
		/// method does not throw exceptions for missing or invalid files.</remarks>
		/// <returns>A list of <see cref="BackupJob"/> objects representing the saved backup jobs. Returns an empty list if no backup
		/// jobs are found or if an error occurs while loading.</returns>
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

		/// <summary>
		/// Saves the specified collection of backup jobs to persistent storage.
		/// </summary>
		/// <param name="jobs">The list of <see cref="BackupJob"/> instances to be saved. Cannot be null.</param>
		/// <exception cref="Exception">Thrown if an error occurs while saving the backup jobs to storage.</exception>
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
