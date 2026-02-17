using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
	public class ChangeLanguageCommand : ICommand
	{

		public string GetI18nKey() => "menu_language";

		public int GetID() => 6;
		
		public void Execute()
		{
			Console.Clear();
			Console.WriteLine("{0}: ", I18n.Instance.GetString("language_select"));
			var langProperties = I18n.Instance.LoadLanguagesProperties();
			int i = 0;
			foreach (var lang in langProperties)
			{
				Console.WriteLine("{0} - {1}", ++i, lang.Value["@language_name"]);
			}
			var choice = ConsoleExt.ReadDec();

			i = 0;
			foreach (var lang in langProperties)
			{
				i++;
				if (i == choice)
				{
					I18n.Instance.SetLanguage(lang.Key);
					Console.WriteLine(I18n.Instance.GetString("language_changed"));
					break;
				}
			}
		}
	}
}