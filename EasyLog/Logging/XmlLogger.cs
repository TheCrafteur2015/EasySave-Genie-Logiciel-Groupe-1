using EasyLog.Data;
using System.Xml.Serialization;

namespace EasyLog.Logging
{
	public class XmlLogger(string path) : AbstractLogger(path)
	{
		private readonly object _lock = new();

		public override string GetExtension() => "xml";

		public override void Log(LogEntry message)
		{
			List<LogEntry> logs = new List<LogEntry>();
			if (File.Exists(LogFile))
			{
				string existingContent = File.ReadAllText(LogFile);
				if (!string.IsNullOrWhiteSpace(existingContent))
				{
					var serializer = new XmlSerializer(typeof(List<LogEntry>));
					using var reader = new StringReader(existingContent);
					logs = serializer.Deserialize(reader) as List<LogEntry> ?? new();
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