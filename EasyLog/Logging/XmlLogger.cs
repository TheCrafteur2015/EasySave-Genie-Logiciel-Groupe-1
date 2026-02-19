using EasyLog.Data;
using System.Xml.Serialization;

namespace EasyLog.Logging
{
	/// <summary>
	/// Logger implementation that outputs logs in XML format.
	/// </summary>
	/// <remarks>
	/// This logger serializes log entries into an XML structure. It handles reading existing logs,
	/// deserializing them to append the new entry, and serializing the updated list back to the file.
	/// </remarks>
	/// <param name="path">The directory path where log files will be created.</param>
	public class XmlLogger(string path) : AbstractLogger(path)
	{
		private readonly object _lock = new();

		/// <summary>
		/// Gets the file extension for XML log files.
		/// </summary>
		/// <returns>The string "xml".</returns>
		public override string GetExtension() => "xml";

		/// <summary>
		/// Writes a log entry to the XML log file.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe for writing. It reads the existing XML content,
		/// deserializes it into a list of LogEntries, adds the new message, and overwrites the file
		/// with the updated serialized XML data.
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
					var serializer = new XmlSerializer(typeof(List<LogEntry>));
					using var reader = new StringReader(existingContent);
					logs = serializer.Deserialize(reader) as List<LogEntry> ?? [];
				}
			}
			logs.Add(message);
			lock (_lock)
			{
				var serializer = new XmlSerializer(typeof(List<LogEntry>));
				using var writer = new StringWriter();
				serializer.Serialize(writer, logs);
				File.WriteAllText(LogFile, writer.ToString());
			}
		}
	}
}