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
            // 1. Support Ligne de Commande (CLI)
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
                try 
                { 
                    choice = ConsoleExt.ReadDec(); 
                } 
                catch(FormatException) 
                {
                    Console.WriteLine(I18n.Instance.GetString("invalid_choice"));
                    continue;
                }

                // Préparation de l'écran pour les commandes d'exécution (ex: 2, 3, 10)
                if (choice == 2 || choice == 3 || choice == 10)
                {
                    PrepareConsoleForMonitoring();
                }

                if (!context.ExecuteCommand(choice))
                    Console.WriteLine(I18n.Instance.GetString("invalid_choice"));

                StopMonitoring();
                Console.WriteLine($"\n{I18n.Instance.GetString("press_enter")}");
                _ = Console.ReadLine();

                if (BackupManager.GetBM().LatestSignal == Signal.Exit) break;
            }
        }

        public static void ProcessCommandLine(string[] args)
        {
            try
            {
                string argument = args[0];
                var bm = BackupManager.GetBM();
                
                PrepareConsoleForMonitoring();

                if (argument.Contains('-'))
                {
                    var parts = argument.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                    {
                        bm.ExecuteJobRange(start, end, DisplayProgress);
                    }
                }
                else if (argument.Contains(';'))
                {
                    var ids = argument.Split(';').Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToArray();
                    if (ids.Length > 0) bm.ExecuteJobList(ids, DisplayProgress);
                }
                else if (int.TryParse(argument, out int singleId))
                {
                    bm.ExecuteJob(singleId, DisplayProgress);
                }

                StopMonitoring();
                Console.WriteLine("\nExecution completed!");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", I18n.Instance.GetString("error"), e.Message);
            }
        }

        // --- FONCTIONNALITÉS DASHBOARD (v3.0) ---

        public static void PrepareConsoleForMonitoring()
        {
            lock (_consoleLock)
            {
                Console.Clear();
                Console.CursorVisible = false;
                Console.WriteLine("=== EasySave Active Dashboard ===");
                Console.WriteLine($"{"Name",-20} | {"Progress",-25} | {"Status",-10} | {"Current File"}");
                Console.WriteLine(new string('-', Console.WindowWidth - 1));

                _baseLineIndex = Console.CursorTop;
                _jobLines.Clear();
                
                // On réserve une zone pour les instructions en bas
                Console.SetCursorPosition(0, Console.WindowHeight - 2);
                Console.Write("[CONTROLS] P: Pause | R: Resume | S: Stop | Esc: Quit");
            }
        }

        public static void StopMonitoring()
        {
            lock (_consoleLock)
            {
                int lastLine = _baseLineIndex + _jobLines.Count;
                if (lastLine < Console.BufferHeight) Console.SetCursorPosition(0, lastLine + 2);
                Console.CursorVisible = true;
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
                if (currentRow >= Console.BufferHeight - 3) return; // Sécurité pour ne pas écraser les contrôles

                Console.SetCursorPosition(0, currentRow);

                int barSize = 15;
                double percent = Math.Clamp(state.ProgressPercentage, 0, 100);
                int filled = (int)((percent / 100.0) * barSize);
                string bar = "[" + new string('=', filled) + new string(' ', barSize - filled) + "]";

                string name = (state.BackupName.Length > 18) ? state.BackupName[..15] + "..." : state.BackupName;
                string status = state.State.ToString();
                string file = Path.GetFileName(state.CurrentSourceFile) ?? "";
                if (file.Length > 30) file = "..." + file[^27..];

                string line = $"{name,-20} | {bar} {percent,5:F1}% | {status,-10} | {file}";
                
                // Nettoyage de la fin de ligne
                int padding = Console.WindowWidth - line.Length - 1;
                if (padding > 0) line += new string(' ', padding);

                var oldColor = Console.ForegroundColor;
                if (state.State == State.Error) Console.ForegroundColor = ConsoleColor.Red;
                else if (state.State == State.Completed) Console.ForegroundColor = ConsoleColor.Green;
                else if (state.State == State.Paused) Console.ForegroundColor = ConsoleColor.Yellow;

                Console.Write(line);
                Console.ForegroundColor = oldColor;
            }
        }

        // --- FONCTIONNALITÉS INTERACTION TEMPS RÉEL (Feature) ---

        public static void MonitorJobs(List<Task> tasks)
        {
            // Cette boucle surveille les touches pendant que les Task s'exécutent en arrière-plan
            while (!Task.WaitAll(tasks.ToArray(), 50))
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    lock (_consoleLock)
                    {
                        if (key == ConsoleKey.Escape) break;

                        if (key == ConsoleKey.P || key == ConsoleKey.R || key == ConsoleKey.S)
                        {
                            // On place le curseur sur la ligne de commande temporaire
                            Console.SetCursorPosition(0, Console.WindowHeight - 1);
                            Console.Write(new string(' ', Console.WindowWidth - 1)); // Clear line
                            Console.SetCursorPosition(0, Console.WindowHeight - 1);
                            
                            Console.Write($"Action ({key}) > ID (0=ALL): ");
                            if (int.TryParse(Console.ReadLine(), out int id))
                            {
                                var bm = BackupManager.GetBM();
                                switch (key)
                                {
                                    case ConsoleKey.P: if (id == 0) bm.PauseAllJobs(); else bm.PauseJob(id); break;
                                    case ConsoleKey.R: if (id == 0) bm.ResumeAllJobs(); else bm.ResumeJob(id); break;
                                    case ConsoleKey.S: if (id == 0) bm.StopAllJobs(); else bm.StopJob(id); break;
                                }
                            }
                            // On efface la ligne de saisie après l'action
                            Console.SetCursorPosition(0, Console.WindowHeight - 1);
                            Console.Write(new string(' ', Console.WindowWidth - 1));
                        }
                    }
                }
            }
        }
    }
}