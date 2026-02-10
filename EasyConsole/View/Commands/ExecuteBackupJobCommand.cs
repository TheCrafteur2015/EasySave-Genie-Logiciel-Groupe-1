using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	public class ExecuteBackupJobCommand : ICommand
	{

		public int GetID() => 2;

		public void Execute()
		{
			Console.Clear();
			
			// Afficher la liste des processus de sauvegarde
			CommandContext.Instance.ExecuteCommand(4);
			
			Console.Write("{0}: ", I18n.Instance.GetString("execute_id"));

			int id = ConsoleExt.ReadDec();

			try
			{
				if (id > 0)
				{
					BackupManager.GetBM().ExecuteJob(id, ConsoleView.DisplayProgress);
					Console.WriteLine(I18n.Instance.GetString("execute_success"));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: ", I18n.Instance.GetString("execute_failure") + e.Message);
			}
		}

		public string GetI18nKey() => "menu_execute";

	}
}
