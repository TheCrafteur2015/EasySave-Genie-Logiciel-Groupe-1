using EasySave.Backup;
using EasySave.View.Localization;

namespace EasySave.View
{
	/// <summary>
	/// Console View - Handles user interface (View in MVVM)
	/// <para>
	/// The ConsoleView class act like the View in your software architecture.
	/// It is responsible of menu displaying and get user input.
	/// </para>
	/// </summary>
	public class ConsoleView
	{
        /// <summary>
        /// Initialization of the new instance for the console view.
		/// Load the unique instance needed for the functioning of the application system.
        /// </summary>
        public ConsoleView()
		{
			// Initialization of Singleton instances
			_ = BackupManager.GetBM();
			_ = BackupManager.GetLogger();
			_ = I18n.Instance;
		}

        /// <summary>
        /// Start the application system in console mode.
        /// </summary>
        /// <param name="args">Arguments input from the command line when the application is launched.</param>
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

		/// <summary>
		/// Parses and processes command-line arguments to execute one or more backup jobs based on the specified input
		/// format.
		/// </summary>
		/// <remarks>The method supports three input formats for specifying backup jobs: a single integer for one job,
		/// a hyphen-separated range for multiple jobs, or a semicolon-separated list for specific jobs. If the input does not
		/// match any of these formats or is invalid, no jobs are executed. Any exceptions encountered during processing are
		/// caught and an error message is displayed.</remarks>
		/// <param name="args">An array of command-line arguments.
		/// <item><description>"1-3" : range of ID </description></item>
		/// <item><description>"1;3" : list of ID</description></item>
		/// <item><description>"1" : unique ID</description></item>
		/// </param>
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

        /// <summary>
        /// Displays the main application menu to the console; the user has to select an option.
        /// </summary>
        /// <remarks>The menu text is localized based on the current language settings. This method clears the console
        /// before displaying the menu options.</remarks>
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

		/// <summary>
		/// Prompts the user to create a new backup job by entering the required information and adds the job to the backup
		/// manager.
		/// </summary>
		/// <remarks>This method interacts with the user via the console to collect the backup job's name, source,
		/// target, and type. The job is only created if all required fields are provided. Success or failure messages are
		/// displayed to the user based on the outcome.</remarks>
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

		/// <summary>
		/// Prompts the user to select and execute a backup job by its ID.
		/// </summary>
		/// <remarks>Displays a list of available backup jobs and requests the user to enter the identifier of the job
		/// to execute. Provides feedback on the success or failure of the operation. This method is intended for interactive
		/// console use and does not return a value.</remarks>
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

		/// <summary>
		/// Executes all configured backup jobs and displays progress and status messages to the console.
		/// </summary>
		/// <remarks>This method clears the console before starting execution and provides user feedback on the
		/// outcome of the backup operations. It is intended for use in interactive console applications and should not be
		/// called from non-interactive contexts.</remarks>
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

		/// <summary>
		/// Displays a list of all configured backup jobs and their details to the console.
		/// </summary>
		/// <remarks>If no backup jobs are configured, a message indicating that the list is empty is displayed. The
		/// output includes each job's identifier, name, source and target directories, backup type, current state, and the
		/// time of the last execution. All output is localized based on the current language settings.</remarks>
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

		/// <summary>
		/// Prompts the user to select and delete a backup job from the list of available backup jobs.
		/// </summary>
		/// <remarks>This method displays all existing backup jobs, requests the user to enter the identifier of the
		/// job to delete, and attempts to remove the specified job. A confirmation or error message is displayed based on the
		/// outcome. The method expects user input from the console and does not return a value.</remarks>
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
		/// <summary>
		/// Prompts the user to select a language and updates the application's language setting based on the user's choice.
		/// </summary>
		/// <remarks>This method clears the console, displays a list of available languages, and waits for the user to
		/// select one. The application's language is changed immediately after a valid selection. The method pauses for a key
		/// press before returning. This method is intended for interactive console applications and should be called from the
		/// main user interface thread.</remarks>

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

		/// <summary>
		/// Displays the current progress of an operation to the console using localized messages.
		/// </summary>
		/// <remarks>This method outputs progress details such as the number of files processed, total size,
		/// percentage completed, and the name of the current file, if available. All messages are localized using the
		/// application's internationalization resources. Intended for use in console applications.</remarks>
		/// <param name="state">The current progress state containing information about files, size, percentage completed, and the current file
		/// being processed. Cannot be null.</param>
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

		/// <summary>
		/// Returns the localized string representation of the specified backup type.
		/// </summary>
		/// <param name="type">The type of backup to convert to a localized string.</param>
		/// <returns>A localized string that describes the specified backup type.</returns>
		private string GetBackupTypeString(BackupType type)
		{
			return type == BackupType.Complete 
				? I18n.Instance.GetString("type_complete") 
				: I18n.Instance.GetString("type_differential");
		}

		/// <summary>
		/// Returns a localized string that describes the specified backup state.
		/// </summary>
		/// <remarks>The returned string is localized based on the application's current language settings. If the
		/// specified state is not recognized, a string representing the inactive state is returned.</remarks>
		/// <param name="state">The backup state for which to retrieve the localized description.</param>
		/// <returns>A localized string representing the specified backup state.</returns>
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
