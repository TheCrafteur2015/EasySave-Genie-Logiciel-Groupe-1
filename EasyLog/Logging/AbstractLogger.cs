using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.Logging
{
	public abstract class AbstractLogger : ILogger
	{

		public string Path { get; private set; }

		public AbstractLogger(string path)
		{
			Path = path;
			Init();
		}

		private void Init()
		{
			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);
		}

		public string GetFile() => System.IO.Path.Combine(Path, $"{DateTime.Now:yyyy-MM-dd}.log");

        public abstract void Log(Level level, string message);

        public abstract void LogError(Exception e);

    }
}
