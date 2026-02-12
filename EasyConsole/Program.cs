using EasySave.Backup;
using EasyConsole.View;

namespace EasyConsole
{
    /// <summary>
    /// Main entry point of the EasySave application
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main method of the project executed at the launch of the application.
        /// It initialize the console view and manages the global exception of the program.
        /// </summary>
        /// <param name="args">Array of arguments passed on the command line at the start of the application.</param>
        static void Main(string[] args)
        {
            // TODO: Retirer la ligne suivante
            var view = new ConsoleView();
            try
            {
                ConsoleView.Run(args);
            }
            catch (Exception e)
            {
                BackupManager.GetLogger().LogError(e);
            }
        }
    }
}
