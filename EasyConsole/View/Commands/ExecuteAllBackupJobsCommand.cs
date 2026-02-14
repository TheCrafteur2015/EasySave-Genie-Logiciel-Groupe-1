using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    /// <summary>
    /// Command to execute all configured backup jobs sequentially.
    /// </summary>
    public class ExecuteAllBackupJobsCommand : ICommand
    {
        /// <summary>
        /// Executes the workflow for running all backup jobs.
        /// </summary>
        /// <remarks>
        /// This method clears the console, notifies the user that execution is starting,
        /// and triggers the execution of all jobs via the BackupManager.
        /// It uses the ConsoleView.DisplayProgress method to show real-time progress.
        /// </remarks>
        public void Execute()
        {
            Console.Clear();
            Console.WriteLine(I18n.Instance.GetString("execute_all_start"));

            try
            {
                BackupManager.GetBM().ExecuteAllJobs(ConsoleView.DisplayProgress);
                Console.WriteLine(I18n.Instance.GetString("execute_success"));
            }
            catch (Exception e)
            {
                Console.WriteLine(I18n.Instance.GetString("execute_failure") + e.Message);
            }
        }

        /// <summary>
        /// Gets the localization key for the "Execute all backup jobs" menu item.
        /// </summary>
        /// <returns>The string key "menu_execute_all".</returns>
        public string GetI18nKey() => "menu_execute_all";


        /// <summary>
        /// Gets the unique identifier for the execute all jobs command.
        /// </summary>
        /// <returns>The integer ID 3.</returns>
        public int GetID() => 3;
    }
}