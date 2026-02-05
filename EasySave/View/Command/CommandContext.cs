using EasySave.View.Commands;
using EasySave.View.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.View.Command
{
	public class CommandContext
	{

		private static CommandContext? _instance;

		public static CommandContext Instance
		{
			get
			{
				_instance ??= new CommandContext();
				return _instance;
			}
		}

		private readonly Dictionary<int, ICommand> commandList;

		private CommandContext()
		{
			commandList = [];
			InitCommands();
		}

		private void InitCommands()
		{
			ICommand[] commands = [
				new CreateBackupJobCommand(),
				new ExecuteBackupJobCommand(),
				new ExecuteAllBackupJobsCommand(),
				new ListBackupJobsCommand(),
				new DeleteBackupJobCommand(),
				new ChangeLanguageCommand(),
				new ExitCommand(),
			];
			
			foreach(var command in commands)
				this.commandList[command.GetID()] = command;
		}

		public bool ExecuteCommand(int id)
		{
			if (this.commandList.TryGetValue(id, out ICommand? command))
			{
				if (command == null)
					return false;
				command.Execute();
				return true;
			}
			return false;
		}

		public void DisplayCommands()
		{
			Console.Clear();
			Console.WriteLine(I18n.Instance.GetString("menu_title"));
			Console.WriteLine();

			foreach (var command in this.commandList)
				Console.WriteLine("{0}. {1}", command.Key, I18n.Instance.GetString(command.Value.GetI18nKey()));

			Console.WriteLine();
			Console.Write(I18n.Instance.GetString("menu_choice"));
		}

	}
}
