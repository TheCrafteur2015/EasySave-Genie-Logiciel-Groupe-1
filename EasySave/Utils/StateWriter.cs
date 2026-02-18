using EasySave.Backup;
using Newtonsoft.Json;

namespace EasySave.Utils
{
	/// <summary>
	/// Writes real-time state information to a JSON file
	/// </summary>
	public class StateWriter
	{
		private readonly string _stateFilePath;
		private readonly object _lockObject = new();
		private readonly Dictionary<string, ProgressState> _currentStates = [];

		public StateWriter(string stateDirectory)
		{
			if (!Directory.Exists(stateDirectory))
			{
				Directory.CreateDirectory(stateDirectory);
			}

			_stateFilePath = Path.Combine(stateDirectory, "state.json");
		}

		/// <summary>
		/// Updates the progress state for a specific backup operation.
		/// </summary>
		/// <remarks>This method is thread-safe. The updated state is persisted to storage after the
		/// operation completes.</remarks>
		/// <param name="state">The new progress state to associate with the backup. Cannot be null. The <see
		/// cref="ProgressState.BackupName"/> property is used to identify which backup's state to update.</param>
		public void UpdateState(ProgressState state)
		{
			lock (_lockObject)
			{
				_currentStates[state.BackupName] = state;
				WriteStatesToFile();
			}
		}

		/// <summary>
		/// Removes the backup state associated with the specified backup name.
		/// </summary>
		/// <remarks>If the specified backup name does not exist, no action is taken. This method is
		/// thread-safe.</remarks>
		/// <param name="backupName">The name of the backup whose state should be removed. Cannot be null.</param>
		public void RemoveState(string backupName)
		{
			lock (_lockObject)
			{
				if (_currentStates.ContainsKey(backupName))
				{
					_currentStates.Remove(backupName);
					WriteStatesToFile();
				}
			}
		}

		/// <summary>
		/// Writes the current collection of progress states to a file in JSON format.
		/// </summary>
		/// <remarks>This method overwrites the existing file at the specified path with the latest
		/// progress state data. The file path and the collection of states must be properly initialized before calling
		/// this method.</remarks>
		private void WriteStatesToFile()
		{
			var statesList = new List<ProgressState>(_currentStates.Values);
			string jsonContent = JsonConvert.SerializeObject(statesList, Formatting.Indented);
			File.WriteAllText(_stateFilePath, jsonContent);
		}
	}
}