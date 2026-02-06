using EasySave.Backup;
using EasySave.Extensions;
using EasySave.View.Command;
using EasySave.View.Localization;

namespace EasySave.View
{
	/// <summary>
	/// Console View - Handles user interface (View in MVVM)
	/// </summary>
	public class ConsoleView
	{

		public ConsoleView()
		{
			// Initialization of Singleton instances
			_ = BackupManager.GetBM();
			_ = BackupManager.GetLogger();
			_ = I18n.Instance;
		}

		public void Run(string[] args)
		{
			// Check for command line arguments
			if (args.Length > 0)
			{
                ProcessCommandLine(args);
				return;
			}

			var context = CommandContext.Instance;

			// Interactive menu mode
			while (true)
			{
				BackupManager.GetBM().TransmitSignal(Signal.Continue);
				context.DisplayCommands();
				int choice = ConsoleExt.ReadDec();
				if (!context.ExecuteCommand(choice))
					Console.WriteLine(I18n.Instance.GetString("invalid_choice"));
				Console.WriteLine($"\n{I18n.Instance.GetString("press_enter")}");
				_ = Console.ReadLine();

				if (BackupManager.GetBM().LatestSignal == Signal.None)
					throw new InvalidOperationException("Oops! This should not happen!");

				if (BackupManager.GetBM().LatestSignal == Signal.Exit)
				{
					break;
				}
			}
		}

		public static void ProcessCommandLine(string[] args)
		{
			try
			{
				// Parse command line: EasySave.exe 1-3 or EasySave.exe 1;3
				string argument = args[0];

				if (argument.Contains('-'))
				{
					// Range: 1-3
					var parts = argument.Split('-');
					if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
					{
						Console.WriteLine($"Executing backup jobs {start} to {end}...");
						BackupManager.GetBM().ExecuteJobRange(start, end, DisplayProgress);
						Console.WriteLine("Execution completed!");
					}
				}
				else if (argument.Contains(';'))
				{
					// List: 1;3;5
					var parts = argument.Split(';');
					var ids = parts.Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToArray();
					
					if (ids.Length > 0)
					{
						Console.WriteLine($"Executing backup jobs: {string.Join(", ", ids)}...");
						BackupManager.GetBM().ExecuteJobList(ids, DisplayProgress);
						Console.WriteLine("Execution completed!");
					}
				}
				else if (int.TryParse(argument, out int singleId))
				{
					// Single job
					Console.WriteLine($"Executing backup job {singleId}...");
					BackupManager.GetBM().ExecuteJob(singleId, DisplayProgress);
					Console.WriteLine("Execution completed!");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("error"), e.Message);
			}
		}

		public static void DisplayProgress(ProgressState state)
		{
			Console.WriteLine($"\n{I18n.Instance.GetString("progress_active")}");
			Console.WriteLine(string.Format(I18n.Instance.GetString("progress_files"), 
				state.TotalFiles - state.FilesRemaining, state.TotalFiles));
			Console.WriteLine(string.Format(I18n.Instance.GetString("progress_size"), 
				state.TotalSize - state.SizeRemaining, state.TotalSize));
			Console.WriteLine(string.Format(I18n.Instance.GetString("progress_percentage"), 
				state.ProgressPercentage));
			
			if (!string.IsNullOrEmpty(state.CurrentSourceFile))
			{
				Console.WriteLine(string.Format(I18n.Instance.GetString("progress_current"), 
					Path.GetFileName(state.CurrentSourceFile)));
			}
		}
		
	}
}
