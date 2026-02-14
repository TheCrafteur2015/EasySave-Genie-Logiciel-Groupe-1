using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    /// <summary>
    /// Command to change the application's interface language.
    /// </summary>
    public class ChangeLanguageCommand : ICommand
    {

        /// <summary>
        /// Gets the localization key for the "Change language" menu item.
        /// </summary>
        /// <returns>The string key "menu_language".</returns>
        public string GetI18nKey() => "menu_language";

        /// <summary>
        /// Gets the unique identifier for the change language command.
        /// </summary>
        /// <returns>The integer ID 6.</returns>
        public int GetID() => 6;

        /// <summary>
        /// Executes the language change workflow.
        /// </summary>
        /// <remarks>
        /// This method displays the list of available languages loaded from resources,
        /// prompts the user to select one, and updates the global I18n configuration.
        /// </remarks>
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
            //_ = Console.ReadKey();
        }
    }
}