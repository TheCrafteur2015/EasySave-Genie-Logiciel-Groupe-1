using EasyConsole.View.Command;
using EasySave.Backup;
using EasySave.View.Localization;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyConsole.View.Commands
{
	public partial class ConfigurationCommand : ICommand
	{

		public void Execute()
		{
			var i18n = I18n.Instance;
			Console.WriteLine("{0}:", i18n.GetString("config_list"));
			Console.WriteLine("\tlist                - {0}", i18n.GetString("config_list_desc"));
			Console.WriteLine("\tget <{0}>         - {1}", i18n.GetString("config_param"), i18n.GetString("config_get_desc"));
			Console.WriteLine("\tset <{0}> <{1}> - {2}", i18n.GetString("config_param"), i18n.GetString("config_value"), i18n.GetString("config_set_desc"));
			Console.WriteLine("\topen                - {0}", i18n.GetString("config_open_desc"));
			Console.WriteLine("\texit                - {0}", i18n.GetString("config_exit_desc"));

			string? action;
			bool loop = true;
			var configManager = BackupManager.GetBM().ConfigManager;
			do
			{
				Console.Write("> ");
				action = Console.ReadLine() ?? string.Empty;
				switch (action)
				{
					case "list":
						Dictionary<string, object> configs = configManager.GetConfigDictionary();
						foreach (var config in configs)
						{
							Console.WriteLine("{0}={1}", config.Key, config.Value);
						}
						break;
					case var s when GetRegex().IsMatch(s):
						var param1 = GetRegex().Match(s).Groups["param"].Value;
						try
						{
							Console.WriteLine("{0}={1}", param1, configManager.GetConfig<object?>(param1));
						} catch (ArgumentException e)
						{
							Console.Error.WriteLine(e.Message);
							BackupManager.GetLogger().LogError(e);
						}
						break;
					case var s when SetRegex().IsMatch(s):
						var param2   = SetRegex().Match(s).Groups["param"].Value;
						string value = SetRegex().Match(s).Groups["value"].Value;
						try
						{
							bool success = false;

							using var doc = JsonDocument.Parse(value);
							if (BoolRegex().IsMatch(value))
								success = configManager.SetConfig<bool>(param2, doc.RootElement.GetBoolean());
							else if (IntRegex().IsMatch(value))
								success = configManager.SetConfig<int>(param2, doc.RootElement.GetInt32());
							else if (DoubleRegex().IsMatch(value))
								success = configManager.SetConfig<double>(param2, doc.RootElement.GetDouble());
							else
								success = configManager.SetConfig<string>(param2, value);

							if (success)
								Console.WriteLine("Successfully updated param {0} value to {1}", param2, value);
						}
						catch (ArgumentException e)
						{
							Console.Error.WriteLine(e.Message);
							BackupManager.GetLogger().LogError(e);
						}
						break;
					case "open":
						try
						{
							Console.WriteLine(i18n.GetString("config_open_msg"));
							Process.Start(new ProcessStartInfo()
							{
								FileName = configManager.configFilePath,
								UseShellExecute = true
							});
						}
						catch (Exception e)
						{
							Console.WriteLine(i18n.GetString("config_open_fail"));
							BackupManager.GetLogger().LogError(e);
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

		public string GetI18nKey() => "menu_settings";

		public int GetID() => 7;

		[GeneratedRegex(@"^get (?<param>\w+)$")]
		private static partial Regex GetRegex();

		[GeneratedRegex(@"^set (?<param>\w+) (?<value>.+)$")]
		private static partial Regex SetRegex();

		[GeneratedRegex(@"^([tT][rR][uU][eE]|[fF][aA][lL][sS][eE])$")]
		private static partial Regex BoolRegex();

		[GeneratedRegex(@"^-?\d+$")]
		private static partial Regex IntRegex();

		[GeneratedRegex(@"^-?\d+\.\d+$")]
		private static partial Regex DoubleRegex();
	}
}