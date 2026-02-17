using EasyLog.Logging;
using EasySave.Backup;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Utils
{
	/// <summary>
	/// Manages application configuration and backup jobs persistence
	/// </summary>
	public class ConfigurationManager
	{

		/// <summary>
		/// Static options used for JSON serialization.
		/// </summary>
		private static readonly JsonSerializerOptions JSON_OPTIONS = new()
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() },
			IncludeFields = true
		};

		public readonly string configFilePath;
		public readonly string savedBackupJobPath;

		private readonly object _saveLock = new();

		/// <summary>
		/// Gets the dynamic configuration values for the current instance.
		/// </summary>
		/// <remarks>The returned object provides access to configuration settings whose structure may vary at
		/// runtime. Use dynamic member access to retrieve specific configuration values as needed.</remarks>
		private readonly Dictionary<string, object> configValues;

		private readonly List<string> readOnlyVariables = ["Version", "PriorityExtensions"];

		/// <summary>
		/// Indicates wether the configuration variables have been edited and are required to save.
		/// </summary>
		private bool isDirty = false;

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
			if (!Directory.Exists(configDirectory))
				Directory.CreateDirectory(configDirectory);

			configFilePath     = Path.Combine(configDirectory, ResourceManager.CONFIG_FILENAME);
			savedBackupJobPath = Path.Combine(configDirectory, ResourceManager.BACKUP_FILENAME);

			if (!File.Exists(configFilePath) || new FileInfo(configFilePath).Length == 0)
				File.WriteAllText(configFilePath, ResourceManager.ReadResourceFile(ResourceManager.DEFAULT_CONFIG_FILENAME));

			string jsonContent = File.ReadAllText(configFilePath);
			configValues = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? [];

			// Migrate configuration if needed
			MigrateConfigurationIfNeeded();
	}

		/// <summary>
		/// Migrates the configuration file to the latest version by merging existing values
		/// with new default values for any missing keys.
		/// </summary>
		private void MigrateConfigurationIfNeeded()
		{
			var defaultConfig = (JsonSerializer.Deserialize<Dictionary<string, object>>(
				ResourceManager.ReadResourceFile(ResourceManager.DEFAULT_CONFIG_FILENAME)
			) ?? []) ?? throw new FileNotFoundException("Couldn't find packaged resource: " + ResourceManager.DEFAULT_CONFIG_FILENAME);

			if (GetConfig<Version>("Version").CompareTo(Version.Create(defaultConfig["Version"] as string)) == -1)
			{
				foreach (var kvp in defaultConfig)
				{
					if (!configValues.ContainsKey(kvp.Key))
					{
						configValues.Add(kvp.Key, kvp.Value);
						MarkDirty();
					}
				}
			}

			if (isDirty)
				configValues["Version"] = defaultConfig["Version"];
		}

		public T GetConfig<T>(string key, T? defaultValue = default)
		{
			T? ret = default;
			if (configValues.TryGetValue(key, out var value))
			{
				if (key == "Version" && typeof(T) == typeof(Version))
					return (T) (object) Version.Create(value as string);

				ret = value switch
				{
					T t => t,
					JsonElement je => je.Deserialize<T>(),
					_ => (T) Convert.ChangeType(value, typeof(T)) ?? defaultValue
				} ?? defaultValue;
			}
			return ret ?? throw new ArgumentException("This configuration key doesn't exists!"); ;
		}

		public Dictionary<string, object> GetConfigDictionary()
		{
			return JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(configValues)) ?? [];
		}

		public bool SetConfig<T>(string key, T? value = default)
		{
			if (value == null)
				return false;
			if (!configValues.ContainsKey(key))
				return false;
			if (readOnlyVariables.Contains(key))
			{
				BackupManager.GetLogger().Log(new() { Level = Level.Warning, Message = $"User attempted to edit read-only field: {key}" });
				return false;
			}
			configValues[key] = value;
			MarkDirty();
			return true;
		}

		public void MarkDirty()
		{
			isDirty = true;
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
			if (!File.Exists(savedBackupJobPath))
			{
				return [];
			}

			try
			{
				string jsonContent = File.ReadAllText(savedBackupJobPath);
				var jobs = JsonSerializer.Deserialize<List<BackupJob>>(jsonContent);
				return jobs ?? [];
			}
			catch (Exception ex)
			{
				// Log l'erreur pour debug
				System.Diagnostics.Debug.WriteLine($"Error loading backup jobs: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");

				// Supprimer le fichier corrompu
				try
				{
					File.Delete(_savedBackupJobPath);
					System.Diagnostics.Debug.WriteLine($"Corrupted file deleted: {_savedBackupJobPath}");
				}
				catch { }
				BackupManager.GetLogger().LogError(ex);
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
                lock (_saveLock)
                {
                    string jsonContent = JsonSerializer.Serialize(jobs, JSON_OPTIONS);
                    File.WriteAllText(savedBackupJobPath, jsonContent);
                }
            }
			catch (Exception ex)
			{
				throw new Exception($"Failed to save configuration: {ex.Message}");
			}
		}

		public void SaveConfiguration()
		{
			if (isDirty)
			{
				File.WriteAllText(configFilePath, JsonSerializer.Serialize(configValues, JSON_OPTIONS));
				BackupManager.GetLogger().Log(new() { Level = Level.Info, Message = "Saving configuration file..." });
			} else
			{
				BackupManager.GetLogger().Log(new() { Level = Level.Info, Message = "Configuration file untouched" });
			}
		}
	}
}
