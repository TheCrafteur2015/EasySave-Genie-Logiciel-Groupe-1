using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Logger
{
	class SimpleLogger(string path) : AbstractLogger(path)
	{

        private readonly object _lock = new();

        public override void Log(Level level, string message)
        {
            lock (_lock)
            {
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {level}: {message}\n");
			}
        }

        public override void LogError(Exception e)
        {
			lock (_lock)
            {
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {Level.Error}: {e.Message}\n");
				File.AppendAllText(Path, $"[{DateTime.Now:G}] {Level.Error}: Stacktrace: {e.StackTrace}\n");
			}
		}

    }
}
