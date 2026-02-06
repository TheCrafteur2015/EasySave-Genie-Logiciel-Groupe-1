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
    public class CreateBackupJobCommand : ICommand
    {

		public int GetID() => 1;

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

		public string GetI18nKey() => "menu_create";
    }
}
