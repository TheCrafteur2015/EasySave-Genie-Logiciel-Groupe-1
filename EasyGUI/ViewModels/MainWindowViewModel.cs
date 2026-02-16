using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using EasySave.View.Localization; // Nécessaire pour I18n
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace EasyGUI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        // --- GESTION DE LA TRADUCTION DYNAMIQUE ---
        // Cette propriété permet d'utiliser {Binding L[key]} dans le XAML
        public I18n L => I18n.Instance;

        public string Greeting { get; } = "Welcome to EasySave!";

        private readonly BackupManager _backupManager;

        // Liste des jobs affichée dans l'UI
        public ObservableCollection<BackupJob> BackupJobs { get; set; }

        // Navigation
        private string _currentView = "Menu";
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // --- PROPRIÉTÉS DE FORMULAIRE ---
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

        // --- COMMANDES ---
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
            // 1. Récupération du Singleton BackupManager
            _backupManager = BackupManager.GetBM();

            // 2. Initialisation de la liste observable
            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);

            // 3. Initialisation des commandes
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

        // --- MÉTHODES ---

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

        private void RefreshBackupJobs()
        {
            BackupJobs.Clear();
            foreach (var job in _backupManager.GetAllJobs())
            {
                BackupJobs.Add(job);
            }
        }

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
                StatusMessage = "✓ Job created successfully!"; // Tu peux aussi utiliser L["create_success"] ici si tu veux

                // Petit délai pour l'UX (optionnel, peut nécessiter async/await pour être propre, ici simplifié)
                // Thread.Sleep(1000); 

                CurrentView = "Menu";
                StatusMessage = "";
            }
            else
            {
                StatusMessage = "✗ Failed to create job. Check inputs or max limit.";
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
                    int jobId = SelectedJob.Id;
                    string jobName = SelectedJob.Name;
                    StatusMessage = $"Executing '{jobName}'...";

                    // Exécution synchrone (bloque l'UI, idéalement à mettre dans un Task.Run pour la V3)
                    _backupManager.ExecuteJob(jobId, progress =>
                    {
                        // Callback de progression
                        // Note: Pour mettre à jour l'UI depuis un autre thread, il faudrait Dispatcher.UIThread.Invoke
                        // Mais ici BackupManager est synchrone dans la V1/V2, donc ça passe.
                        int filesProcessed = progress.TotalFiles - progress.FilesRemaining;
                        StatusMessage = $"Running: {progress.ProgressPercentage:F1}% - {filesProcessed}/{progress.TotalFiles}";
                    });

                    RefreshBackupJobs();
                    SelectedJob = BackupJobs.FirstOrDefault(j => j.Id == jobId); // Restaurer sélection

                    StatusMessage = $"✓ '{jobName}' completed!";
                    IsExecuting = false;
                }
                catch (Exception ex)
                {
                    IsExecuting = false;
                    StatusMessage = $"✗ Error: {ex.Message}";
                }
            }
            else
            {
                StatusMessage = "Please select a job.";
            }
        }

        private void ExecuteAllBackupJobs()
        {
            try
            {
                StatusMessage = "Executing all jobs...";
                IsExecuting = true;

                _backupManager.ExecuteAllJobs(progress =>
                {
                    StatusMessage = $"Running: {progress.BackupName} - {progress.ProgressPercentage:F1}%";
                });

                RefreshBackupJobs();
                StatusMessage = "✓ All jobs completed!";
                IsExecuting = false;
            }
            catch (Exception ex)
            {
                IsExecuting = false;
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
                    StatusMessage = $"✓ '{jobName}' deleted!";
                    // Thread.Sleep(1000);
                    CurrentView = "Menu";
                    StatusMessage = "";
                }
                else
                {
                    StatusMessage = "✗ Failed to delete.";
                }
            }
            else
            {
                StatusMessage = "Select a job first.";
            }
        }

        private void ChangeLanguage()
        {
            // On charge la langue actuelle dans le sélecteur
            // Si la langue est "fr_fr", l'index est 1, sinon 0
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;

            CurrentView = "ChangeLanguage";
            StatusMessage = "";
        }

        private void ApplyLanguage()
        {
            // 0 = English (en_us), 1 = French (fr_fr)
            string code = SelectedLanguage == 0 ? "en_us" : "fr_fr";

            try
            {
                // 1. Changer la langue dans le singleton
                I18n.Instance.SetLanguage(code);

                // 2. Notifier la vue que la propriété "L" a changé
                // Cela force le re-binding de tous les textes {Binding L[...]}
                OnPropertyChanged(nameof(L));

                StatusMessage = code == "en_us" ? "Language set to English" : "Langue définie sur Français";

                // Optionnel : Retourner au menu ou rester sur la page
                // CurrentView = "Menu"; 
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}