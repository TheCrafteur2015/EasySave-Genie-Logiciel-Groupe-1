using EasySave.Backup;
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

			// Interactive menu mode
			bool running = true;
			while (running)
			{
				DisplayMenu();
				_ = BackupManager.GetBM();
				string? choice = Console.ReadLine();
				switch (choice)
				{
					case "1":
						CreateBackupJob();
						break;
					case "2":
						ExecuteBackupJob();
						break;
					case "3":
						ExecuteAllBackupJobs();
						break;
					case "4":
						ListBackupJobs();
						break;
					case "5":
						DeleteBackupJob();
						break;
					case "6":
						ChangeLanguage();
						break;
					case "7":
						running = false;
						break;
					default:
						Console.WriteLine(I18n.Instance.GetString("invalid_choice"));
						break;
				}

				if (running)
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("press_enter")}");
					Console.ReadLine();
				}
			}
		}

		public void ProcessCommandLine(string[] args)
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
			catch (Exception ex)
			{
				Console.WriteLine($"{I18n.Instance.GetString("error")}{ex.Message}");
			}
		}

		public void DisplayMenu()
		{
			Console.Clear();
			Console.WriteLine(I18n.Instance.GetString("menu_title"));
			Console.WriteLine();
			Console.WriteLine(I18n.Instance.GetString("menu_create"));
			Console.WriteLine(I18n.Instance.GetString("menu_execute"));
			Console.WriteLine(I18n.Instance.GetString("menu_execute_all"));
			Console.WriteLine(I18n.Instance.GetString("menu_list"));
			Console.WriteLine(I18n.Instance.GetString("menu_delete"));
			Console.WriteLine(I18n.Instance.GetString("menu_language"));
			Console.WriteLine(I18n.Instance.GetString("menu_exit"));
			Console.WriteLine();
			Console.Write(I18n.Instance.GetString("menu_choice"));
		}

		private void CreateBackupJob()
		{
			Console.Clear();
			Console.Write(I18n.Instance.GetString("create_name"));
			string? name = Console.ReadLine();

			Console.Write(I18n.Instance.GetString("create_source"));
			string? source = Console.ReadLine();

			Console.Write(I18n.Instance.GetString("create_target"));
			string? target = Console.ReadLine();

			Console.Write(I18n.Instance.GetString("create_type"));
			string? typeChoice = Console.ReadLine();

			BackupType type = typeChoice == "2" ? BackupType.Differential : BackupType.Complete;

			if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
			{
				bool success = BackupManager.GetBM().AddJob(name, source, target, type);
				if (success)
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("create_success")}");
				}
				else
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("create_failure")}");
				}
			}
		}

		private void ExecuteBackupJob()
		{
			Console.Clear();
			ListBackupJobs();
			Console.Write($"\n{I18n.Instance.GetString("execute_id")}");
			
			if (int.TryParse(Console.ReadLine(), out int id))
			{
				try
				{
					BackupManager.GetBM().ExecuteJob(id, DisplayProgress);
					Console.WriteLine($"\n{I18n.Instance.GetString("execute_success")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("execute_failure")}{ex.Message}");
				}
			}
		}

		private void ExecuteAllBackupJobs()
		{
			Console.Clear();
			Console.WriteLine(I18n.Instance.GetString("execute_all_start"));
			
			try
			{
				BackupManager.GetBM().ExecuteAllJobs(DisplayProgress);
				Console.WriteLine($"\n{I18n.Instance.GetString("execute_success")}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\n{I18n.Instance.GetString("execute_failure")}{ex.Message}");
			}
		}

		private void ListBackupJobs()
		{
			Console.WriteLine(I18n.Instance.GetString("list_title"));
			
			var jobs = BackupManager.GetBM().GetAllJobs();
			
			if (!jobs.Any())
			{
				Console.WriteLine(I18n.Instance.GetString("list_empty"));
				return;
			}

			foreach (var job in jobs)
			{
				Console.WriteLine($"\n{I18n.Instance.GetString("list_id")}{job.Id}");
				Console.WriteLine($"{I18n.Instance.GetString("list_name")}{job.Name}");
				Console.WriteLine($"{I18n.Instance.GetString("list_source")}{job.SourceDirectory}");
				Console.WriteLine($"{I18n.Instance.GetString("list_target")}{job.TargetDirectory}");
				Console.WriteLine($"{I18n.Instance.GetString("list_type")}{GetBackupTypeString(job.Type)}");
				Console.WriteLine($"{I18n.Instance.GetString("list_state")}{GetBackupStateString(job.State)}");
				Console.WriteLine($"{I18n.Instance.GetString("list_last_exec")}{(job.LastExecution.ToString("yyyy-MM-dd HH:mm:ss") ?? I18n.Instance.GetString("never"))}");
			}
		}

		private void DeleteBackupJob()
		{
			Console.Clear();
			ListBackupJobs();
			Console.Write($"\n{I18n.Instance.GetString("delete_id")}");
			
			if (int.TryParse(Console.ReadLine(), out int id))
			{
				bool success = BackupManager.GetBM().DeleteJob(id);
				if (success)
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("delete_success")}");
				}
				else
				{
					Console.WriteLine($"\n{I18n.Instance.GetString("delete_failure")}");
				}
			}
		}

		private void ChangeLanguage()
		{
			Console.Clear();
			Console.WriteLine(I18n.Instance.GetString("language_select"));
			var langProperties = I18n.Instance.LoadLanguagesProperties();
			int i = 0;
			foreach (var lang in langProperties)
			{
				Console.WriteLine("{0} - {1}", ++i, lang.Value["@language_name"]);
			}
			var choice = Console.Read();
			
			if (choice.GetTypeCode() == TypeCode.Int32)
			{
				i = 0;
				foreach (var lang in langProperties)
				{
					i++;
					if (48 + i == choice)
					{
						I18n.Instance.SetLanguage(lang.Key);
						Console.WriteLine(I18n.Instance.GetString("language_changed"));
						break;
					}
				}
			}
			_ = Console.ReadKey();
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

		private string GetBackupTypeString(BackupType type)
		{
			return type == BackupType.Complete 
				? I18n.Instance.GetString("type_complete") 
				: I18n.Instance.GetString("type_differential");
		}

		private string GetBackupStateString(State state)
		{
			return state switch
			{
				State.Active => I18n.Instance.GetString("state_active"),
				State.Completed => I18n.Instance.GetString("state_completed"),
				State.Error => I18n.Instance.GetString("state_error"),
				_ => I18n.Instance.GetString("state_inactive")
			};
		}
	}
}
