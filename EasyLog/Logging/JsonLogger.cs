using System.Text.Json;
using EasyLog.Data;

namespace EasyLog.Logging
{
	public class JsonLogger(string path) : AbstractLogger(path)
	{
		private readonly object _lock = new();

		public override string GetExtension() => "json";
		
		public override void Log(LogEntry message)
		{
			List<LogEntry> logs = new List<LogEntry>();
			if (File.Exists(LogFile))
			{
				string existingContent = File.ReadAllText(LogFile);
				if (!string.IsNullOrWhiteSpace(existingContent))
				{
					logs = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? new List<LogEntry>();
				}
			}
			logs.Add(message);
			lock (_lock)
			{
				File.WriteAllText(LogFile, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
			}
		}
	}
}