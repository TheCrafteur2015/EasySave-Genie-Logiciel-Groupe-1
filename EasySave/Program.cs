using EasySave.Views;

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
            view.Run(args);
        }
    }
}
