using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    public class ExecuteAllBackupJobsCommand : ICommand
    {
        public int GetID() => 3;
        public string GetI18nKey() => "menu_execute_all";

        public void Execute()
        {
            // Note: Le Console.Clear() est maintenant fait par ConsoleView.PrepareConsoleForMonitoring()
            // appelé dans le Run() principal. On ne le fait pas ici pour éviter de tout effacer.

            try
            {
                // On lance les sauvegardes. L'affichage se fait via le callback DisplayProgress
                BackupManager.GetBM().ExecuteAllJobs(ConsoleView.DisplayProgress);

                // IMPORTANT : On dit à la vue de placer le curseur SOUS le tableau
                // avant d'écrire le message de succès.
                ConsoleView.StopMonitoring();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(I18n.Instance.GetString("execute_success"));
                Console.ResetColor();
            }
            catch (Exception e)
            {
                ConsoleView.StopMonitoring();
                Console.WriteLine(I18n.Instance.GetString("execute_failure") + e.Message);
            }
        }
    }
}