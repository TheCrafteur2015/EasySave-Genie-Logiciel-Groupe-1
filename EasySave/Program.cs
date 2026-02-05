using EasySave.Backup;
using EasySave.View;

namespace EasySave
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
            var view = new ConsoleView();
            try
            {
                view.Run(args);
            }
            catch (Exception e)
            {
                BackupManager.GetLogger().LogError(e);
            }
        }
    }
}
