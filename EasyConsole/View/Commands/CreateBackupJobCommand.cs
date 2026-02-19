using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	/// <summary>
	/// Command to create a new backup job.
	/// Handles user input for job details and registers the job via the BackupManager.
	/// </summary>
	public class CreateBackupJobCommand : ICommand
	{

		/// <summary>
		/// Gets the unique identifier for the create backup job command.
		/// </summary>
		/// <returns>The integer ID 1.</returns>
		public int GetID() => 1;

		/// <summary>
		/// Executes the workflow for creating a new backup job.
		/// </summary>
		/// <remarks>
		/// This method prompts the user for the backup job name, source directory,
		/// target directory, and backup type (Complete or Differential).
		/// It then attempts to add the job to the BackupManager and displays a success or failure message.
		/// </remarks>
		public void Execute()
		{
			Console.Clear();
			Console.Write("{0}: ", I18n.Instance.GetString("create_name"));
			string? name = Console.ReadLine();

			Console.Write("{0}: ", I18n.Instance.GetString("create_source"));
			string? source = Console.ReadLine();

			Console.Write("{0}: ", I18n.Instance.GetString("create_target"));
			string? target = Console.ReadLine();

			Console.Write("{0}: ", I18n.Instance.GetString("create_type"));
			int typeChoice = ConsoleExt.ReadDec();

			BackupType type = typeChoice == 2 ? BackupType.Differential : BackupType.Complete;
			string key = BackupManager.GetBM().AddJob(name, source, target, type) ? "success" : "failure";
			Console.WriteLine(I18n.Instance.GetString($"create_{key}"));
		}

		/// <summary>
		/// Gets the localization key for the "Create backup job" menu item.
		/// </summary>
		/// <returns>The string key "menu_create".</returns>
		public string GetI18nKey() => "menu_create";
	}
}