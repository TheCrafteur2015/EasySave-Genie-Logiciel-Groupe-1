using EasySave.Backup;
using EasySave.View;

namespace EasySave
{
    /// <summary>
    /// Main entry point of the EasySave application
    /// </summary>
    class Program
    {
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
