using EasySave.Backup;
using EasyConsole.View.Command;

namespace EasyConsole.View.Commands
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