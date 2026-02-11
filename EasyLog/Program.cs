using EasyLog.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyLog
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//LogEntry[]? logs = JsonSerializer.Deserialize<LogEntry[]>(File.ReadAllText(@"C:\Users\Gabriel Roche\Projets\test.json"));
			//if (logs != null)
			//    foreach (LogEntry log in logs)
			//        Console.WriteLine(log.Name);

			Console.WriteLine(JsonSerializer.Serialize(
				new LogEntry(),
				new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
			 ));
		}
	}
}
