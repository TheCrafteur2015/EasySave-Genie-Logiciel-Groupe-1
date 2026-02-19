using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View
{
	/// <summary>
	/// Represents the main view controller for the Console application.
	/// Manages user interaction, command-line arguments, and the real-time monitoring dashboard.
	/// </summary>
	public class ConsoleView
	{
		/// <summary>
		/// Stores the console line index assigned to each backup job name for dynamic updates.
		/// </summary>
		private static readonly Dictionary<string, int> _jobLines = [];

		/// <summary>
		/// The starting line index in the console where the job list begins.
		/// </summary>
		private static int _baseLineIndex = 0;

		/// <summary>
		/// Lock object used to synchronize console output and prevent text overlapping in multi-threaded scenarios.
		/// </summary>
		private static readonly object _consoleLock = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsoleView"/> class.
		/// Triggers the initialization of core managers and localization.
		/// </summary>
		public ConsoleView()
		{
			_ = BackupManager.GetBM();
			_ = BackupManager.GetLogger();
			_ = I18n.Instance;
		}

		/// <summary>
		/// Starts the main application loop or processes command-line arguments.
		/// </summary>
		/// <param name="args">The command-line arguments passed to the application.</param>
		public static void Run(string[] args)
		{
			if (args.Length > 0)
			{
				ProcessCommandLine(args);
				return;
			}

			var context = CommandContext.Instance;
			while (true)
			{
				BackupManager.GetBM().TransmitSignal(Signal.Continue);
				context.DisplayCommands();

				int choice;
				try
				{
					choice = ConsoleExt.ReadDec();
				}
				catch (FormatException)
				{
					Console.WriteLine(I18n.Instance.GetString("invalid_choice"));
					continue;
				}

				if (choice == 2 || choice == 3 || choice == 10)
				{
					PrepareConsoleForMonitoring();
				}

				if (!context.ExecuteCommand(choice))
					Console.WriteLine(I18n.Instance.GetString("invalid_choice"));

				StopMonitoring();
				Console.WriteLine($"\n{I18n.Instance.GetString("press_enter")}");
				_ = Console.ReadLine();

				if (BackupManager.GetBM().LatestSignal == Signal.Exit) break;
			}
		}

		/// <summary>
		/// Parses and executes commands provided via command-line arguments.
		/// Supports ranges (1-3), lists (1;3;5), or single IDs.
		/// </summary>
		/// <param name="args">The array of string arguments.</param>
		public static void ProcessCommandLine(string[] args)
		{
			try
			{
				string argument = args[0];
				var bm = BackupManager.GetBM();

				PrepareConsoleForMonitoring();

				if (argument.Contains('-'))
				{
					var parts = argument.Split('-');
					if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
					{
						bm.ExecuteJobRange(start, end, DisplayProgress);
					}
				}
				else if (argument.Contains(';'))
				{
					var ids = argument.Split(';').Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToArray();
					if (ids.Length > 0) bm.ExecuteJobList(ids, DisplayProgress);
				}
				else if (int.TryParse(argument, out int singleId))
				{
					bm.ExecuteJob(singleId, DisplayProgress);
				}

				StopMonitoring();
				Console.WriteLine("\nExecution completed!");
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("error"), e.Message);
				BackupManager.GetLogger().LogError(e);
			}
		}


		/// <summary>
		/// Sets up the console interface for the real-time monitoring dashboard.
		/// Clears the screen, hides the cursor, and draws the table header.
		/// </summary>
		public static void PrepareConsoleForMonitoring()
		{
			lock (_consoleLock)
			{
				Console.Clear();
				Console.CursorVisible = false;
				Console.WriteLine("=== EasySave Active Dashboard ===");
				Console.WriteLine($"{"Name",-20} | {"Progress",-25} | {"Status",-10} | {"Current File"}");
				Console.WriteLine(new string('-', Console.WindowWidth - 1));

				_baseLineIndex = Console.CursorTop;
				_jobLines.Clear();
				Console.SetCursorPosition(0, Console.WindowHeight - 2);
				Console.Write("[CONTROLS] P: Pause | R: Resume | S: Stop | Esc: Quit");
			}
		}

		/// <summary>
		/// Restores the console state after monitoring is finished.
		/// Re-enables the cursor and moves it to the bottom of the display.
		/// </summary>
		public static void StopMonitoring()
		{
			lock (_consoleLock)
			{
				int lastLine = _baseLineIndex + _jobLines.Count;
				if (lastLine < Console.BufferHeight) Console.SetCursorPosition(0, lastLine + 2);
				Console.CursorVisible = true;
			}
		}

		/// <summary>
		/// Updates the progress information for a specific backup job on the dashboard.
		/// </summary>
		/// <param name="state">The current progress state of the backup job.</param>
		public static void DisplayProgress(ProgressState state)
		{
			lock (_consoleLock)
			{
				if (!_jobLines.ContainsKey(state.BackupName))
				{
					_jobLines[state.BackupName] = _baseLineIndex + _jobLines.Count;
				}

				int currentRow = _jobLines[state.BackupName];
				if (currentRow >= Console.BufferHeight - 3) return; 

				Console.SetCursorPosition(0, currentRow);

				int barSize = 15;
				double percent = Math.Clamp(state.ProgressPercentage, 0, 100);
				int filled = (int)((percent / 100.0) * barSize);
				string bar = "[" + new string('=', filled) + new string(' ', barSize - filled) + "]";

				string name = (state.BackupName.Length > 18) ? state.BackupName[..15] + "..." : state.BackupName;
				string status = state.State.ToString();
				string file = Path.GetFileName(state.CurrentSourceFile) ?? "";
				if (file.Length > 30) file = "..." + file[^27..];

				string line = $"{name,-20} | {bar} {percent,5:F1}% | {status,-10} | {file}";

				int padding = Console.WindowWidth - line.Length - 1;
				if (padding > 0) line += new string(' ', padding);

				var oldColor = Console.ForegroundColor;
				if (state.State == State.Error) Console.ForegroundColor = ConsoleColor.Red;
				else if (state.State == State.Completed) Console.ForegroundColor = ConsoleColor.Green;
				else if (state.State == State.Paused) Console.ForegroundColor = ConsoleColor.Yellow;

				Console.Write(line);
				Console.ForegroundColor = oldColor;
			}
		}


		/// <summary>
		/// Monitors keyboard input while backup tasks are running in the background.
		/// Allows the user to pause, resume, or stop jobs in real-time.
		/// </summary>
		/// <param name="tasks">A list of running tasks to monitor.</param>
		public static void MonitorJobs(List<Task> tasks)
		{
			while (!Task.WaitAll(tasks.ToArray(), 50))
			{
				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(true).Key;

					lock (_consoleLock)
					{
						if (key == ConsoleKey.Escape) break;

						if (key == ConsoleKey.P || key == ConsoleKey.R || key == ConsoleKey.S)
						{
							Console.SetCursorPosition(0, Console.WindowHeight - 1);
							Console.Write(new string(' ', Console.WindowWidth - 1));
							Console.SetCursorPosition(0, Console.WindowHeight - 1);

							Console.Write($"Action ({key}) > ID (0=ALL): ");
							if (int.TryParse(Console.ReadLine(), out int id))
							{
								var bm = BackupManager.GetBM();
								switch (key)
								{
									case ConsoleKey.P: if (id == 0) bm.PauseAllJobs(); else bm.PauseJob(id); break;
									case ConsoleKey.R: if (id == 0) bm.ResumeAllJobs(); else bm.ResumeJob(id); break;
									case ConsoleKey.S: if (id == 0) bm.StopAllJobs(); else bm.StopJob(id); break;
								}
							}
							Console.SetCursorPosition(0, Console.WindowHeight - 1);
							Console.Write(new string(' ', Console.WindowWidth - 1));
						}
					}
				}
			}
		}
	}
}