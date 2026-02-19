using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	public class ExecuteAllBackupJobsCommand : ICommand
	{
		public int GetID() => 3;
		public string GetI18nKey() => "menu_execute_all";

		public void Execute()
		{
			// Note: Le Console.Clear() est géré par ConsoleView.PrepareConsoleForMonitoring()
			// dans le Run() principal avant d'arriver ici.

			try
			{
				// 1. On lance en asynchrone pour pouvoir interagir (Feature: interactions_temps_reel)
				var tasks = BackupManager.GetBM().ExecuteAllJobsAsync(ConsoleView.DisplayProgress);

				// 2. On lance le moniteur interactif (P/R/S) qui attend la fin des tâches (Feature)
				ConsoleView.MonitorJobs(tasks);

				// 3. Une fois les tâches terminées ou stoppées, on replace le curseur en bas (v3.0)
				ConsoleView.StopMonitoring();

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(I18n.Instance.GetString("execute_success"));
				Console.ResetColor();
			}
			catch (Exception e)
			{
				// On s'assure de libérer le curseur même en cas de crash
				ConsoleView.StopMonitoring();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"{I18n.Instance.GetString("execute_failure")}: {e.Message}");
				Console.ResetColor();
				BackupManager.GetLogger().LogError(e);
			}
		}
	}
}