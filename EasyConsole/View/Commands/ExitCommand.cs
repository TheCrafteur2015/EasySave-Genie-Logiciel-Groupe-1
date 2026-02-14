using EasySave.Backup;
using EasyConsole.View.Command;

namespace EasyConsole.View.Commands
{
    /// <summary>
    /// Command to exit the application.
    /// </summary>
    public class ExitCommand : ICommand
    {
        /// <summary>
        /// Gets the localization key for the "Exit" menu item.
        /// </summary>
        /// <returns>The string key "menu_exit".</returns>
        public string GetI18nKey() => "menu_exit";

        /// <summary>
        /// Gets the unique identifier for the exit command.
        /// </summary>
        /// <returns>The integer ID 7.</returns>
        public int GetID() => 7;

        /// <summary>
        /// Executes the exit workflow.
        /// </summary>
        /// <remarks>
        /// This method transmits an Exit signal to the BackupManager, 
        /// allowing the application to terminate the main execution loop gracefully.
        /// </remarks>
        public void Execute()
        {
            BackupManager.GetBM().TransmitSignal(Signal.Exit);
        }
    }
}