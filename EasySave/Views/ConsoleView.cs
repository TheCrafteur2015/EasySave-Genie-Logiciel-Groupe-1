using EasySave.Models;
using EasySave.ViewModels;
using EasySave.Views.Localization;
using System;
using System.Linq;

namespace EasySave.Views
{
    /// <summary>
    /// Console View - Handles user interface (View in MVVM)
    /// </summary>
    public class ConsoleView
    {
        private readonly BackupManager _backupManager;
        private readonly LocalizationService _localization;

        public ConsoleView()
        {
            _backupManager = BackupManager.GetInstance();
            _localization = new LocalizationService();
        }

        public void Run(string[] args)
        {
            // Check for command line arguments
            if (args.Length > 0)
            {
                ProcessCommandLine(args);
                return;
            }

            // Interactive menu mode
            bool running = true;
            while (running)
            {
                DisplayMenu();
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        CreateBackupJob();
                        break;
                    case "2":
                        ExecuteBackupJob();
                        break;
                    case "3":
                        ExecuteAllBackupJobs();
                        break;
                    case "4":
                        ListBackupJobs();
                        break;
                    case "5":
                        DeleteBackupJob();
                        break;
                    case "6":
                        ChangeLanguage();
                        break;
                    case "7":
                        running = false;
                        break;
                    default:
                        Console.WriteLine(_localization.GetString("invalid_choice"));
                        break;
                }

                if (running)
                {
                    Console.WriteLine($"\n{_localization.GetString("press_enter")}");
                    Console.ReadLine();
                }
            }
        }

        public void ProcessCommandLine(string[] args)
        {
            try
            {
                // Parse command line: EasySave.exe 1-3 or EasySave.exe 1;3
                string argument = args[0];

                if (argument.Contains('-'))
                {
                    // Range: 1-3
                    var parts = argument.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                    {
                        Console.WriteLine($"Executing backup jobs {start} to {end}...");
                        _backupManager.ExecuteJobRange(start, end, DisplayProgress);
                        Console.WriteLine("Execution completed!");
                    }
                }
                else if (argument.Contains(';'))
                {
                    // List: 1;3;5
                    var parts = argument.Split(';');
                    var ids = parts.Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToArray();
                    
                    if (ids.Length > 0)
                    {
                        Console.WriteLine($"Executing backup jobs: {string.Join(", ", ids)}...");
                        _backupManager.ExecuteJobList(ids, DisplayProgress);
                        Console.WriteLine("Execution completed!");
                    }
                }
                else if (int.TryParse(argument, out int singleId))
                {
                    // Single job
                    Console.WriteLine($"Executing backup job {singleId}...");
                    _backupManager.ExecuteJob(singleId, DisplayProgress);
                    Console.WriteLine("Execution completed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_localization.GetString("error")}{ex.Message}");
            }
        }

        public void DisplayMenu()
        {
            Console.Clear();
            Console.WriteLine(_localization.GetString("menu_title"));
            Console.WriteLine();
            Console.WriteLine(_localization.GetString("menu_create"));
            Console.WriteLine(_localization.GetString("menu_execute"));
            Console.WriteLine(_localization.GetString("menu_execute_all"));
            Console.WriteLine(_localization.GetString("menu_list"));
            Console.WriteLine(_localization.GetString("menu_delete"));
            Console.WriteLine(_localization.GetString("menu_language"));
            Console.WriteLine(_localization.GetString("menu_exit"));
            Console.WriteLine();
            Console.Write(_localization.GetString("menu_choice"));
        }

        private void CreateBackupJob()
        {
            Console.Clear();
            Console.Write(_localization.GetString("create_name"));
            string? name = Console.ReadLine();

            Console.Write(_localization.GetString("create_source"));
            string? source = Console.ReadLine();

            Console.Write(_localization.GetString("create_target"));
            string? target = Console.ReadLine();

            Console.Write(_localization.GetString("create_type"));
            string? typeChoice = Console.ReadLine();

            BackupType type = typeChoice == "2" ? BackupType.Differential : BackupType.Complete;

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
            {
                bool success = _backupManager.AddJob(name, source, target, type);
                if (success)
                {
                    Console.WriteLine($"\n{_localization.GetString("create_success")}");
                }
                else
                {
                    Console.WriteLine($"\n{_localization.GetString("create_failure")}");
                }
            }
        }

        private void ExecuteBackupJob()
        {
            Console.Clear();
            ListBackupJobs();
            Console.Write($"\n{_localization.GetString("execute_id")}");
            
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                try
                {
                    _backupManager.ExecuteJob(id, DisplayProgress);
                    Console.WriteLine($"\n{_localization.GetString("execute_success")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n{_localization.GetString("execute_failure")}{ex.Message}");
                }
            }
        }

        private void ExecuteAllBackupJobs()
        {
            Console.Clear();
            Console.WriteLine(_localization.GetString("execute_all_start"));
            
            try
            {
                _backupManager.ExecuteAllJobs(DisplayProgress);
                Console.WriteLine($"\n{_localization.GetString("execute_success")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{_localization.GetString("execute_failure")}{ex.Message}");
            }
        }

        private void ListBackupJobs()
        {
            Console.WriteLine(_localization.GetString("list_title"));
            
            var jobs = _backupManager.GetAllJobs();
            
            if (!jobs.Any())
            {
                Console.WriteLine(_localization.GetString("list_empty"));
                return;
            }

            foreach (var job in jobs)
            {
                Console.WriteLine($"\n{_localization.GetString("list_id")}{job.Id}");
                Console.WriteLine($"{_localization.GetString("list_name")}{job.Name}");
                Console.WriteLine($"{_localization.GetString("list_source")}{job.SourceDirectory}");
                Console.WriteLine($"{_localization.GetString("list_target")}{job.TargetDirectory}");
                Console.WriteLine($"{_localization.GetString("list_type")}{GetBackupTypeString(job.Type)}");
                Console.WriteLine($"{_localization.GetString("list_state")}{GetBackupStateString(job.State)}");
                Console.WriteLine($"{_localization.GetString("list_last_exec")}{(job.LastExecution?.ToString("yyyy-MM-dd HH:mm:ss") ?? _localization.GetString("never"))}");
            }
        }

        private void DeleteBackupJob()
        {
            Console.Clear();
            ListBackupJobs();
            Console.Write($"\n{_localization.GetString("delete_id")}");
            
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                bool success = _backupManager.DeleteJob(id);
                if (success)
                {
                    Console.WriteLine($"\n{_localization.GetString("delete_success")}");
                }
                else
                {
                    Console.WriteLine($"\n{_localization.GetString("delete_failure")}");
                }
            }
        }

        private void ChangeLanguage()
        {
            Console.Clear();
            Console.Write(_localization.GetString("language_select"));
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    _localization.SetLanguage("en");
                    Console.WriteLine("\nLanguage changed successfully!");
                    break;
                case "2":
                    _localization.SetLanguage("fr");
                    Console.WriteLine("\nLangue changée avec succès !");
                    break;
            }
        }

        public void DisplayProgress(ProgressState state)
        {
            Console.WriteLine($"\n{_localization.GetString("progress_active")}");
            Console.WriteLine(string.Format(_localization.GetString("progress_files"), 
                state.TotalFiles - state.FilesRemaining, state.TotalFiles));
            Console.WriteLine(string.Format(_localization.GetString("progress_size"), 
                state.TotalSize - state.SizeRemaining, state.TotalSize));
            Console.WriteLine(string.Format(_localization.GetString("progress_percentage"), 
                state.ProgressPercentage));
            
            if (!string.IsNullOrEmpty(state.CurrentSourceFile))
            {
                Console.WriteLine(string.Format(_localization.GetString("progress_current"), 
                    Path.GetFileName(state.CurrentSourceFile)));
            }
        }

        private string GetBackupTypeString(BackupType type)
        {
            return type == BackupType.Complete 
                ? _localization.GetString("type_complete") 
                : _localization.GetString("type_differential");
        }

        private string GetBackupStateString(BackupState state)
        {
            return state switch
            {
                BackupState.Active => _localization.GetString("state_active"),
                BackupState.Completed => _localization.GetString("state_completed"),
                BackupState.Error => _localization.GetString("state_error"),
                _ => _localization.GetString("state_inactive")
            };
        }
    }
}
