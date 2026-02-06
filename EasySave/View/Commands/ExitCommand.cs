using EasySave.Backup;
using EasySave.View.Command;

namespace EasySave.View.Commands
{
	public class ExitCommand : ICommand
	{
		public string GetI18nKey() => "menu_exit";

		public int GetID() => 7;

		public void Execute()
		{
			BackupManager.GetBM().TransmitSignal(Signal.Exit);
		}
	}
}
