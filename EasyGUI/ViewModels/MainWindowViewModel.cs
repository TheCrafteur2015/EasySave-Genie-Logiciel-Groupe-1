using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace EasyGUI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public string Greeting { get; } = "Welcome to EasySave!";

        // Référence vers ton gestionnaire logique (Le Singleton)
        private readonly BackupManager _backupManager;

        // La liste que l'interface graphique va afficher
        // ObservableCollection permet à l'UI de savoir quand on ajoute/supprime une ligne
        public ObservableCollection<BackupJob> BackupJobs { get; set; }

        // Navigation: quelle vue est actuellement affichée
        private string _currentView = "Menu";
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // Propriétés pour les formulaires
        private string _newJobName = "";
        public string NewJobName
        {
            get => _newJobName;
            set => SetProperty(ref _newJobName, value);
        }

        private string _newJobSource = "";
        public string NewJobSource
        {
            get => _newJobSource;
            set => SetProperty(ref _newJobSource, value);
        }

        private string _newJobTarget = "";
        public string NewJobTarget
        {
            get => _newJobTarget;
            set => SetProperty(ref _newJobTarget, value);
        }

        private int _selectedBackupType = 0;
        public int SelectedBackupType
        {
            get => _selectedBackupType;
            set => SetProperty(ref _selectedBackupType, value);
        }

        private BackupJob? _selectedJob;
        public BackupJob? SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        private int _selectedLanguage = 0;
        public int SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isExecuting = false;
        public bool IsExecuting
        {
            get => _isExecuting;
            set => SetProperty(ref _isExecuting, value);
        }

        public ICommand SwitchThemeCommand { get; }
        public ICommand CreateBackupJobCommand { get; }
        public ICommand ExecuteBackupJobCommand { get; }
        public ICommand ExecuteAllBackupJobsCommand { get; }
        public ICommand ListAllBackupJobsCommand { get; }
        public ICommand DeleteBackupJobCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand SaveNewJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand DeleteSelectedJobCommand { get; }
        public ICommand ApplyLanguageCommand { get; }

        public MainWindowViewModel()
        {
            // 1. On récupère ton instance unique
            _backupManager = BackupManager.GetBM();

            // 2. On remplit la liste pour l'interface graphique
            // On charge les jobs existants depuis ton manager
            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);

            // Initialize commands
            SwitchThemeCommand = new RelayCommand<string>(SwitchTheme);
            CreateBackupJobCommand = new RelayCommand(CreateBackupJob);
            ExecuteBackupJobCommand = new RelayCommand(ExecuteBackupJob);
            ExecuteAllBackupJobsCommand = new RelayCommand(ExecuteAllBackupJobs);
            ListAllBackupJobsCommand = new RelayCommand(ListAllBackupJobs);
            DeleteBackupJobCommand = new RelayCommand(DeleteBackupJob);
            ChangeLanguageCommand = new RelayCommand(ChangeLanguage);
            ExitCommand = new RelayCommand(Exit);
            BackToMenuCommand = new RelayCommand(BackToMenu);
            SaveNewJobCommand = new RelayCommand(SaveNewJob);
            ExecuteSelectedJobCommand = new RelayCommand(ExecuteSelectedJob);
            DeleteSelectedJobCommand = new RelayCommand(DeleteSelectedJob);
            ApplyLanguageCommand = new RelayCommand(ApplyLanguage);
        }

        private void SwitchTheme(string? theme)
        {
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = theme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
        }

        // Exemple : Une méthode pour ajouter un job depuis l'UI
        public void CreateNewJob(string name, string source, string target, BackupType type)
        {
            // On appelle ta logique métier
            bool success = _backupManager.AddJob(name, source, target, type);

            if (success)
            {
                // Si ça a marché, on rafraîchit la liste de l'écran
                RefreshBackupJobs();
            }
        }

        private void RefreshBackupJobs()
        {
            BackupJobs.Clear();
            foreach (var job in _backupManager.GetAllJobs())
            {
                BackupJobs.Add(job);
            }
        }

        // Command implementations
        private void BackToMenu()
        {
            CurrentView = "Menu";
            StatusMessage = "";
        }

        private void CreateBackupJob()
        {
            CurrentView = "CreateJob";
            NewJobName = "";
            NewJobSource = "";
            NewJobTarget = "";
            SelectedBackupType = 0;
            StatusMessage = "";
        }

        private void SaveNewJob()
        {
            BackupType type = SelectedBackupType == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = _backupManager.AddJob(NewJobName, NewJobSource, NewJobTarget, type);

            if (success)
            {
                RefreshBackupJobs();
                StatusMessage = $"✓ Backup job '{NewJobName}' created successfully!";

                // Attendre un peu pour que l'utilisateur voie le message
                System.Threading.Thread.Sleep(1500);

                CurrentView = "Menu";
                StatusMessage = "";
            }
            else
            {
                StatusMessage = "✗ Failed to create job. Check that all fields are filled and the maximum number of jobs hasn't been reached.";
            }
        }

        private void ExecuteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ExecuteJob";
        }

        private void ExecuteSelectedJob()
        {
            if (SelectedJob != null)
            {
                try
                {
                    IsExecuting = true;
                    int jobId = SelectedJob.Id; // Sauvegarder l'ID
                    string jobName = SelectedJob.Name; // Sauvegarder le nom
                    StatusMessage = $"Executing backup job '{jobName}'...";

                    _backupManager.ExecuteJob(jobId, progress =>
                    {
                        int filesProcessed = progress.TotalFiles - progress.FilesRemaining;
                        StatusMessage = $"Executing: {progress.CurrentSourceFile} ({filesProcessed}/{progress.TotalFiles}) - {progress.ProgressPercentage:F1}%";
                    });

                    RefreshBackupJobs();

                    // Restaurer la sélection
                    SelectedJob = BackupJobs.FirstOrDefault(j => j.Id == jobId);

                    StatusMessage = $"✓ Backup job '{jobName}' completed successfully!";
                    IsExecuting = false;

                    // Attendre 2 secondes pour que l'utilisateur voie le message
                    System.Threading.Thread.Sleep(2000);

                    CurrentView = "Menu";
                    StatusMessage = "";
                }
                catch (Exception ex)
                {
                    IsExecuting = false;
                    StatusMessage = $"✗ Error: {ex.Message}";
                }
            }
            else
            {
                StatusMessage = "Please select a backup job first.";
            }
        }

        private void ExecuteAllBackupJobs()
        {
            try
            {
                StatusMessage = "Executing all backup jobs...";

                _backupManager.ExecuteAllJobs(progress =>
                {
                    StatusMessage = $"Executing: {progress.BackupName} - {progress.ProgressPercentage:F1}%";
                });

                RefreshBackupJobs();
                StatusMessage = "✓ All backup jobs completed successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
        }

        private void ListAllBackupJobs()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ListJobs";
        }

        private void DeleteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "DeleteJob";
        }

        private void DeleteSelectedJob()
        {
            if (SelectedJob != null)
            {
                string jobName = SelectedJob.Name;
                bool success = _backupManager.DeleteJob(SelectedJob.Id);
                if (success)
                {
                    RefreshBackupJobs();
                    StatusMessage = $"✓ Backup job '{jobName}' deleted successfully!";

                    // Attendre un peu pour que l'utilisateur voie le message
                    System.Threading.Thread.Sleep(1500);

                    CurrentView = "Menu";
                    StatusMessage = "";
                }
                else
                {
                    StatusMessage = "✗ Failed to delete the backup job.";
                }
            }
            else
            {
                StatusMessage = "Please select a backup job to delete.";
            }
        }

        private void ChangeLanguage()
        {
            CurrentView = "ChangeLanguage";
        }

        private void ApplyLanguage()
        {
            // TODO: Implémenter le changement de langue avec le système I18n
            // Pour l'instant on retourne juste au menu
            CurrentView = "Menu";
        }

        private void Exit()
        {
            // Fermer l'application
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}