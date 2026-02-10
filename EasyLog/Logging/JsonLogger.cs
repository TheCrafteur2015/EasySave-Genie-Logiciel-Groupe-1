using System.Text.Json;
using System.IO;
using System.Text;
using EasyLog.Data;

namespace EasyLog.Logging
{
    public class JsonLogger(string path) : AbstractLogger<LogEntry>(path)
    {
        private readonly object _lock = new();

        public override string GetExtension() => "json";

        public override void Log(Level level, LogEntry message)
        {
            string jsonLine = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });

            lock (_lock)
            {
                File.AppendAllText(path, jsonLine + Environment.NewLine);
            }
        }

        public override void LogError(Exception e)
        {
            throw new NotImplementedException();
        }
    }
}