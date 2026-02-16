using EasyConsole.View.Command;
using EasySave.Backup;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	public class ConfigurationCommand : ICommand
	{
		public void Execute()
		{
			var i18n = I18n.Instance;
			Console.WriteLine("{0}:", i18n.GetString("config_list"));
			Console.WriteLine("\tlist - {0}", i18n.GetString("config_list_desc"));
			Console.WriteLine("\tget <{0}> - {1}", i18n.GetString("config_param"), i18n.GetString("config_get_desc"));
			Console.WriteLine("\tset <{0}> <{1}> - {2}", i18n.GetString("config_param"), i18n.GetString("config_value"), i18n.GetString("config_set_desc"));
			Console.WriteLine("\texit - {0}", i18n.GetString("config_exit_desc"));

			string? action;
			bool loop = true;
			do
			{
				Console.Write("> ");
				action = Console.ReadLine();
				switch (action)
				{
					case "list":
						Dictionary<string, object> configs = BackupManager.GetBM().ConfigManager.GetConfigDictionary();
						foreach (var config in configs)
						{
							Console.WriteLine("{0}={1}", config.Key, config.Value);
						}
						break;
					case "exit":
						loop = false;
						break;
					default:
						Console.WriteLine("Unknown command");
						break;
				}
			} while (loop);
		}

		public string GetI18nKey() => "menu_config";

		public int GetID() => 7;
	}

}