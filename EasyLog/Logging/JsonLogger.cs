using System.Text.Json;
using EasyLog.Data;

namespace EasyLog.Logging
{
	/// <summary>
	/// Logger implementation that outputs logs in JSON format.
	/// </summary>
	/// <remarks>
	/// This logger serializes log entries into a JSON array. It handles reading existing logs
	/// to append new entries while maintaining a valid JSON structure.
	/// </remarks>
	/// <param name="path">The directory path where log files will be created.</param>
	public class JsonLogger(string path) : AbstractLogger(path)
	{
		private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

		private readonly object _lock = new();

		/// <summary>
		/// Gets the file extension for JSON log files.
		/// </summary>
		/// <returns>The string "json".</returns>
		public override string GetExtension() => "json";

		/// <summary>
		/// Writes a log entry to the JSON log file.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe for writing. It reads the entire existing log file,
		/// deserializes it into a list, adds the new entry, and rewrites the file with indentation.
		/// </remarks>
		/// <param name="message">The log entry object to be serialized and written.</param>
		public override void Log(LogEntry message)
		{
			List<LogEntry> logs = [];
			if (File.Exists(LogFile))
			{
				string existingContent = File.ReadAllText(LogFile);
				if (!string.IsNullOrWhiteSpace(existingContent))
				{
					logs = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? [];
				}
			}
			logs.Add(message);
			lock (_lock)
			{
				File.WriteAllText(LogFile, JsonSerializer.Serialize(logs, JSON_OPTIONS));
			}
		}
	}
}