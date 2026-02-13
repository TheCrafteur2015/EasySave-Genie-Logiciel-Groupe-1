using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	public class DeleteBackupJobCommand : ICommand
	{

		public string GetI18nKey() => "menu_delete";

		public int GetID() => 5;

		public void Execute()
		{
			Console.Clear();

			// Afficher la liste des processus de sauvegarde
			CommandContext.Instance.ExecuteCommand(4);

			Console.Write("{0}: ", I18n.Instance.GetString("delete_id"));

			if (int.TryParse(Console.ReadLine(), out int id))
			{
				string key = BackupManager.GetBM().DeleteJob(id) ? "success" : "failure";
				Console.WriteLine(I18n.Instance.GetString($"delete_{key}"));
			}
		}
	}
}
