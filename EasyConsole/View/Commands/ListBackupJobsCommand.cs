using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	/// <summary>
	/// Command to list all configured backup jobs.
	/// </summary>
	public class ListBackupJobsCommand : ICommand
	{
		/// <summary>
		/// Executes the workflow for listing backup jobs.
		/// </summary>
		/// <remarks>
		/// This method retrieves the list of jobs from the BackupManager.
		/// If no jobs exist, it displays an empty list message.
		/// Otherwise, it iterates through the jobs and prints their details (ID, Name, Source, Target, Type, State, Last Execution) to the console.
		/// </remarks>
		public void Execute()
		{
			Console.WriteLine("=== {0} ===", I18n.Instance.GetString("list_title"));

			var jobs = BackupManager.GetBM().GetAllJobs();

			if (jobs.Count == 0)
			{
				Console.WriteLine(I18n.Instance.GetString("list_empty"));
				return;
			}

			foreach (var job in jobs)
			{
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_id"), job.Id);
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_name"), job.Name);
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_source"), job.SourceDirectory);
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_target"), job.TargetDirectory);
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_type"), job.Type.GetTranslation());
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_state"), job.State.GetTranslation());
				Console.WriteLine("{0}: {1}", I18n.Instance.GetString("list_last_exec"), job.LastExecution.ToString("yyyy-MM-dd HH:mm:ss") ?? I18n.Instance.GetString("never"));
			}
		}

		/// <summary>
		/// Gets the localization key for the "List backup jobs" menu item.
		/// </summary>
		/// <returns>The string key "menu_list".</returns>
		public string GetI18nKey() => "menu_list";


		/// <summary>
		/// Gets the unique identifier for the list backup jobs command.
		/// </summary>
		/// <returns>The integer ID 4.</returns>
		public int GetID() => 4;
	}
}