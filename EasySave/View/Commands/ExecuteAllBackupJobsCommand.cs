using EasySave.Backup;
using EasySave.View.Command;
using EasySave.View.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.View.Commands
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
