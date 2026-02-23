using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    /// <summary>
    /// Command to execute a specific backup job.
    /// </summary>
    public class ExecuteBackupJobCommand : ICommand
    {

        /// <summary>
        /// Gets the unique identifier for the execute backup job command.
        /// </summary>
        /// <returns>The integer ID 2.</returns>
        public int GetID() => 2;

        /// <summary>
        /// Executes the workflow for running a single backup job.
        /// </summary>
        /// <remarks>
        /// This method clears the console, displays the list of available jobs (by invoking the List command),
        /// prompts the user for the ID of the job to execute, and triggers the execution via the BackupManager.
        /// It handles input validation and displays success or failure messages.
        /// </remarks>
        public void Execute()
        {
            Console.Clear();

            CommandContext.Instance.ExecuteCommand(4);

            Console.Write("{0}: ", I18n.Instance.GetString("execute_id"));

            int id = ConsoleExt.ReadDec();

            try
            {
                if (id > 0)
                {
                    // Utilisation de l'approche asynchrone pour permettre le monitoring (Pause/Stop) dans la vue Console
                    var task = BackupManager.GetBM().ExecuteJobAsync(id, ConsoleView.DisplayProgress);
                    ConsoleView.MonitorJobs([task]);
                    
                    Console.WriteLine(I18n.Instance.GetString("execute_success"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: ", I18n.Instance.GetString("execute_failure") + e.Message);
            }
        }

        /// <summary>
        /// Gets the localization key for the "Execute backup job" menu item.
        /// </summary>
        /// <returns>The string key "menu_execute".</returns>
        public string GetI18nKey() => "menu_execute";

	}
}