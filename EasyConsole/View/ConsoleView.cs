using EasySave.Backup;
using EasySave.Extensions;
using EasyConsole.View.Command;
using EasySave.View.Localization;
using System.Text;

namespace EasyConsole.View
{
    public class ConsoleView
    {
        private static readonly Dictionary<string, int> _jobLines = [];
        private static int _baseLineIndex = 0;
        private static readonly object _consoleLock = new();

        public ConsoleView()
        {
            _ = BackupManager.GetBM();
            _ = BackupManager.GetLogger();
            _ = I18n.Instance;
        }

        public static void Run(string[] args)
        {
            if (args.Length > 0)
            {
                ProcessCommandLine(args);
                return;
            }

            var context = CommandContext.Instance;
            while (true)
            {
                BackupManager.GetBM().TransmitSignal(Signal.Continue);
                context.DisplayCommands();

                int choice;
                try { choice = ConsoleExt.ReadDec(); }
                catch (FormatException) { continue; }

                // Préparation propre de l'écran avant l'exécution
                if (choice == 2 || choice == 3 || choice == 10)
                {
                    PrepareConsoleForMonitoring();
                }

                if (!context.ExecuteCommand(choice))
                    Console.WriteLine(I18n.Instance.GetString("invalid_choice"));

                // --- CORRECTIF IMPORTANT ---
                // On s'assure que le curseur est bien en bas avant d'afficher "Press Enter"
                StopMonitoring();
                Console.WriteLine($"\n{I18n.Instance.GetString("press_enter")}");
                _ = Console.ReadLine();

                if (BackupManager.GetBM().LatestSignal == Signal.Exit) break;
            }
        }

        public static void PrepareConsoleForMonitoring()
        {
            lock (_consoleLock)
            {
                Console.Clear();
                Console.CursorVisible = false; // Cache le curseur pour éviter le clignotement
                Console.WriteLine("=== Active Backup Dashboard (Parallel) ===");
                // En-tête avec espacement fixe pour alignement parfait
                Console.WriteLine($"{"Name",-20} | {"Progress",-25} | {"Status",-10} | {"Current File"}");
                Console.WriteLine(new string('-', Console.WindowWidth - 1));

                _baseLineIndex = Console.CursorTop;
                _jobLines.Clear();
            }
        }

        /// <summary>
        /// Méthode pour sortir proprement du mode monitoring et remettre le curseur en bas
        /// </summary>
        public static void StopMonitoring()
        {
            lock (_consoleLock)
            {
                // On calcule la ligne juste après le dernier job affiché
                int lastLine = _baseLineIndex + _jobLines.Count;
                if (lastLine < Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, lastLine + 1);
                }
                Console.CursorVisible = true; // On réaffiche le curseur
            }
        }

        public static void DisplayProgress(ProgressState state)
        {
            lock (_consoleLock)
            {
                if (!_jobLines.ContainsKey(state.BackupName))
                {
                    _jobLines[state.BackupName] = _baseLineIndex + _jobLines.Count;
                }

                int currentRow = _jobLines[state.BackupName];

                // Protection contre le crash si on redimensionne la fenêtre trop petit
                if (currentRow >= Console.BufferHeight) return;

                Console.SetCursorPosition(0, currentRow);

                // Barre de progression fixe
                int barSize = 15;
                double percent = Math.Clamp(state.ProgressPercentage, 0, 100);
                int filled = (int)((percent / 100.0) * barSize);
                string bar = "[" + new string('=', filled) + new string(' ', barSize - filled) + "]";

                // Troncature et formatage des textes
                string name = (state.BackupName.Length > 18) ? state.BackupName[..15] + "..." : state.BackupName;
                string status = state.State.ToString();
                string file = Path.GetFileName(state.CurrentSourceFile) ?? "";
                if (file.Length > 30) file = "..." + file[^27..]; // Garde la fin du nom de fichier

                // Construction de la ligne
                string line = $"{name,-20} | {bar} {percent,5:F1}% | {status,-10} | {file}";

                // -- CRUCIAL : On remplit le reste de la ligne avec des espaces pour effacer les vieux textes --
                int padding = Console.WindowWidth - line.Length - 1;
                if (padding > 0) line += new string(' ', padding);

                // Gestion des couleurs
                var oldColor = Console.ForegroundColor;
                if (state.State == State.Error) Console.ForegroundColor = ConsoleColor.Red;
                else if (state.State == State.Completed) Console.ForegroundColor = ConsoleColor.Green;

                Console.Write(line);
                Console.ForegroundColor = oldColor;
            }
        }

        // Gardez ProcessCommandLine tel quel, ou ajoutez StopMonitoring() à la fin du try
        public static void ProcessCommandLine(string[] args) { /* ... Code existant ... */ }
    }
}