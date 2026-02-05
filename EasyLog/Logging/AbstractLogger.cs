using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.Logging
{
	public abstract class AbstractLogger : ILogger
	{

		private string path { get; set; }

		public string LogFile { get; private set; }

		public AbstractLogger(string path)
		{
			this.path = path;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			LogFile = this.GetFile();
		}

		public string GetFile() => System.IO.Path.Combine(path, $"{DateTime.Now:yyyy-MM-dd}.log");

        public abstract void Log(Level level, string message);

        public abstract void LogError(Exception e);

    }
}
