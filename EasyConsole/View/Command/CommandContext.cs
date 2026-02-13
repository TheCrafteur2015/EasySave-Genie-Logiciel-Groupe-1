using EasyConsole.View.Commands;
using EasySave.View.Localization;

namespace EasyConsole.View.Command
{
    /// <summary>
    /// Context class for the Command pattern implementation.
    /// It manages the registration, display, and execution of available console commands.
    /// </summary>
    public class CommandContext
    {

        private static CommandContext? _instance;

        /// <summary>
        /// Gets the unique instance of the CommandContext (Singleton pattern).
        /// </summary>
        public static CommandContext Instance
        {
            get
            {
                _instance ??= new CommandContext();
                return _instance;
            }
        }

        private readonly Dictionary<int, ICommand> commandList;

        /// <summary>
        /// Initializes a new instance of the CommandContext class.
        /// Private constructor to enforce Singleton pattern and initialize the command list.
        /// </summary>
        private CommandContext()
        {
            commandList = [];
            InitCommands();
        }

        /// <summary>
        /// Initializes the list of available commands and registers them in the dictionary.
        /// </summary>
        private void InitCommands()
        {
            ICommand[] commands = [
                new CreateBackupJobCommand(),
                new ExecuteBackupJobCommand(),
                new ExecuteAllBackupJobsCommand(),
                new ListBackupJobsCommand(),
                new DeleteBackupJobCommand(),
                new ChangeLanguageCommand(),
                new ExitCommand(),
            ];

            foreach (var command in commands)
                this.commandList[command.GetID()] = command;
        }

        /// <summary>
        /// Executes the command associated with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the command to execute.</param>
        /// <returns>True if the command was found and executed; otherwise, false.</returns>
        public bool ExecuteCommand(int id)
        {
            if (this.commandList.TryGetValue(id, out ICommand? command))
            {
                if (command == null)
                    return false;
                command.Execute();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Displays the main menu with the list of available commands to the console.
        /// </summary>
        public void DisplayCommands()
        {
            Console.Clear();
            Console.WriteLine("=== {0} ===", I18n.Instance.GetString("menu_title"));
            Console.WriteLine();

            foreach (var command in this.commandList)
                Console.WriteLine("{0}. {1}", command.Key, I18n.Instance.GetString(command.Value.GetI18nKey()));

            Console.WriteLine();
            Console.Write("{0}: ", I18n.Instance.GetString("menu_choice"));
        }

    }
}