using EasySave.Backup;
using EasySave.Extensions;
using EasySave.View.Command;
using EasySave.View.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.View.Commands
{
    public class ListBackupJobsCommand : ICommand
    {
        public void Execute()
        {
			Console.WriteLine(I18n.Instance.GetString("list_title"));

			var jobs = BackupManager.GetBM().GetAllJobs();

			if (jobs.Count == 0)
			{
				Console.WriteLine(I18n.Instance.GetString("list_empty"));
				return;
			}

			foreach (var job in jobs)
			{
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_id"),        job.Id);
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_name"),      job.Name);
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_source"),    job.SourceDirectory);
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_target"),    job.TargetDirectory);
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_type"),      job.Type.GetTranslation());
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_state"),     job.State.GetTranslation());
				Console.WriteLine("{0}{1}", I18n.Instance.GetString("list_last_exec"), job.LastExecution.ToString("yyyy-MM-dd HH:mm:ss") ?? I18n.Instance.GetString("never"));
			}
		}

		public string GetI18nKey() => "menu_list";


		public int GetID() => 4;
    }
}
