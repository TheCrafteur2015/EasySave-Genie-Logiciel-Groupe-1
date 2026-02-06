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
        private Dictionary<string, ProgressState> _currentStates = [];

        public StateWriter(string stateDirectory)
        {
            if (!Directory.Exists(stateDirectory))
            {
                Directory.CreateDirectory(stateDirectory);
            }

            _stateFilePath = Path.Combine(stateDirectory, "state.json");
        }

        public void UpdateState(ProgressState state)
        {
            lock (_lockObject)
            {
                _currentStates[state.BackupName] = state;
                WriteStatesToFile();
            }
        }

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

        private void WriteStatesToFile()
        {
            var statesList = new List<ProgressState>(_currentStates.Values);
            string jsonContent = JsonConvert.SerializeObject(statesList, Formatting.Indented);
            File.WriteAllText(_stateFilePath, jsonContent);
        }
    }
}
