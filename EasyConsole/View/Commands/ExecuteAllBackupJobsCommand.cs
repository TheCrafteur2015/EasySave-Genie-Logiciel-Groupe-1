using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    public class ExecuteAllBackupJobsCommand : ICommand
    {
        public void Execute()
        {
			Console.Clear();
			Console.WriteLine(I18n.Instance.GetString("execute_all_start"));

			try
			{
				BackupManager.GetBM().ExecuteAllJobs(ConsoleView.DisplayProgress);
				Console.WriteLine(I18n.Instance.GetString("execute_success"));
			}
			catch (Exception e)
			{
				Console.WriteLine(I18n.Instance.GetString("execute_failure") + e.Message);
			}
		}

		public string GetI18nKey() => "menu_execute_all";


		public int GetID() => 3;
    }
}
