using EasySave.Backup;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Utils
{
	/// <summary>
	/// Manages application configuration and backup jobs persistence
	/// </summary>
	public class ConfigurationManager
	{
		private static readonly JsonSerializerOptions JSON_OPTIONS = new()
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() },
			IncludeFields = true
		};

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

            _configFilePath = Path.Combine(_configDirectory, "config.json");
            _savedBackupJobPath = Path.Combine(_configDirectory, "backups.json");
            if (!File.Exists(_configFilePath) || new FileInfo(_configFilePath).Length == 0)
            {
                File.WriteAllText(_configFilePath, ResourceManager.ReadResourceFile("default.json"));
            }
            string jsonContent = File.ReadAllText(_configFilePath);
            ConfigValues = JsonConvert.DeserializeObject(jsonContent);

        // Migrate configuration if needed
        MigrateConfigurationIfNeeded();
	}

	/// <summary>
	/// Migrates the configuration file to the latest version by merging existing values
	/// with new default values for any missing keys.
	/// </summary>
	private void MigrateConfigurationIfNeeded()
	{
		var defaultConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(
			ResourceManager.ReadResourceFile("default.json"));
		
		if (defaultConfig == null) return;

		var currentConfig = ConfigValues as Newtonsoft.Json.Linq.JObject;
		if (currentConfig == null) return;

		bool configUpdated = false;

		// Add any missing keys from default config
		foreach (var property in defaultConfig.Properties())
		{
			if (currentConfig[property.Name] == null)
			{
				currentConfig[property.Name] = property.Value;
				configUpdated = true;
			}
		}

		// Update version if configuration was migrated
		if (configUpdated)
		{
			var defaultVersion = defaultConfig["Version"];
			if (defaultVersion != null)
			{
				currentConfig["Version"] = defaultVersion;
			}

			// Save the updated configuration
			File.WriteAllText(_configFilePath, currentConfig.ToString(Newtonsoft.Json.Formatting.Indented));
			ConfigValues = currentConfig;
		}
	}

		public dynamic GetConfig(string key)
		{
			return ConfigValues[key] ?? throw new ArgumentException("This configuration key doesn't exists!");
		}

            _configFilePath = Path.Combine(_configDirectory, "config.json");
            _savedBackupJobPath = Path.Combine(_configDirectory, "backups.json");
            if (!File.Exists(_configFilePath) || new FileInfo(_configFilePath).Length == 0)
            {
                File.WriteAllText(_configFilePath, ResourceManager.ReadResourceFile("default.json"));
            }
            string jsonContent = File.ReadAllText(_configFilePath);
            ConfigValues = JsonConvert.DeserializeObject(jsonContent);
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
				var jobs = System.Text.Json.JsonSerializer.Deserialize<List<BackupJob>>(jsonContent);
				return jobs ?? [];
			}
			catch (Exception e)
			{
				BackupManager.GetLogger().LogError(e);
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
				//JSON_OPTIONS
				string jsonContent = System.Text.Json.JsonSerializer.Serialize(jobs, JSON_OPTIONS);
				File.WriteAllText(_savedBackupJobPath, jsonContent);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to save configuration: {ex.Message}");
			}
		}
	}
}
