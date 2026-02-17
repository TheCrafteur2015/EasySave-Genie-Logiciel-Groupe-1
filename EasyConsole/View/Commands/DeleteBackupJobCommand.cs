using EasySave.Backup;
using EasyConsole.View.Command;
using EasySave.View.Localization;

namespace EasyConsole.View.Commands
{
    /// <summary>
    /// Command to delete an existing backup job.
    /// </summary>
    public class DeleteBackupJobCommand : ICommand
    {

        /// <summary>
        /// Gets the localization key for the "Delete backup job" menu item.
        /// </summary>
        /// <returns>The string key "menu_delete".</returns>
        public string GetI18nKey() => "menu_delete";

        /// <summary>
        /// Gets the unique identifier for the delete backup job command.
        /// </summary>
        /// <returns>The integer ID 5.</returns>
        public int GetID() => 5;

        /// <summary>
        /// Executes the workflow for deleting a backup job.
        /// </summary>
        /// <remarks>
        /// This method clears the console, displays the list of existing backup jobs
        /// (by invoking the List command), prompts the user for the ID of the job to delete,
        /// and attempts to remove it via the BackupManager.
        /// </remarks>
        public void Execute()
        {
            Console.Clear();

            // Afficher la liste des processus de sauvegarde
            CommandContext.Instance.ExecuteCommand(4);

            Console.Write("{0}: ", I18n.Instance.GetString("delete_id"));

			if (int.TryParse(Console.ReadLine(), out int id))
			{
				string key = BackupManager.GetBM().DeleteJob(id) ? "success" : "failure";
				Console.WriteLine(I18n.Instance.GetString($"delete_{key}"));
			}
		}
	}
}