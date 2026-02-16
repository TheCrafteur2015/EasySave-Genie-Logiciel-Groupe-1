using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using EasySave.View.Localization; // Nécessaire pour I18n
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyGUI.ViewModels
{
    // Classe pour tracker la progression de chaque job
    public class JobProgressItem : ObservableObject
    {
        private string _jobName = "";
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        private double _progressPercentage = 0;
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _status = "Waiting...";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isCompleted = false;
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        private bool _hasError = false;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }
    }

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

        private int _selectedWindowMode = 1; // 0=Windowed, 1=Maximized (défaut), 2=Fullscreen
        public int SelectedWindowMode
        {
            get
            {
                System.Diagnostics.Debug.WriteLine($"GET SelectedWindowMode = {_selectedWindowMode}");
                return _selectedWindowMode;
            }
            set
            {
                System.Diagnostics.Debug.WriteLine($"SET SelectedWindowMode from {_selectedWindowMode} to {value}");
                SetProperty(ref _selectedWindowMode, value);
            }
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

        private double _currentProgress = 0;
        public double CurrentProgress
        {
            get => _currentProgress;
            set => SetProperty(ref _currentProgress, value);
        }

        private string _currentFileInfo = "";
        public string CurrentFileInfo
        {
            get => _currentFileInfo;
            set => SetProperty(ref _currentFileInfo, value);
        }

        // Collection pour la progression de tous les jobs
        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }

        // Propriétés pour les textes traduits
        private I18n _i18n = I18n.Instance;

        public string MenuTitle => _i18n.GetString("menu_title");
        public string MenuCreate => _i18n.GetString("menu_create");
        public string MenuExecute => _i18n.GetString("menu_execute");
        public string MenuExecuteAll => _i18n.GetString("menu_execute_all");
        public string MenuList => _i18n.GetString("menu_list");
        public string MenuDelete => _i18n.GetString("menu_delete");
        public string MenuLanguage => _i18n.GetString("menu_language");
        public string MenuExit => _i18n.GetString("menu_exit");

        public string ThemeDark => _i18n.GetString("theme_dark");
        public string ThemeLight => _i18n.GetString("theme_light");

        public string CreateTitle => _i18n.GetString("create_title");
        public string CreateName => _i18n.GetString("create_name");
        public string CreateSource => _i18n.GetString("create_source");
        public string CreateTarget => _i18n.GetString("create_target");
        public string CreateType => _i18n.GetString("create_type");
        public string CreateButton => _i18n.GetString("create_button");

        public string ExecuteTitle => _i18n.GetString("execute_title");
        public string ExecuteSelect => _i18n.GetString("execute_select");
        public string ExecuteButton => _i18n.GetString("execute_button");
        public string ExecuteNoJobs => _i18n.GetString("execute_no_jobs");

        public string ListTitle => _i18n.GetString("list_title");
        public string ListId => _i18n.GetString("list_id");
        public string ListName => _i18n.GetString("list_name");
        public string ListSource => _i18n.GetString("list_source");
        public string ListTarget => _i18n.GetString("list_target");

        public string DeleteTitle => _i18n.GetString("delete_title");
        public string DeleteSelect => _i18n.GetString("delete_select");
        public string DeleteButton => _i18n.GetString("delete_button");
        public string DeleteWarning => _i18n.GetString("delete_warning");
        public string DeleteNoJobs => _i18n.GetString("delete_no_jobs");

        public string LanguageTitle => _i18n.GetString("language_title");
        public string LanguageSelect => _i18n.GetString("language_select");
        public string LanguageApply => _i18n.GetString("language_apply");

        public string ButtonCancel => _i18n.GetString("button_cancel");
        public string ButtonBack => _i18n.GetString("button_back");

        public string TypeComplete => _i18n.GetString("type_complete");
        public string TypeDifferential => _i18n.GetString("type_differential");

        // Collection pour les types de backup dans la ComboBox
        public ObservableCollection<string> BackupTypes { get; private set; }

        // Collection pour les modes de fenêtre
        public ObservableCollection<string> WindowModes { get; private set; }

        public ICommand SwitchThemeCommand { get; }
        public ICommand CreateBackupJobCommand { get; }
        public ICommand ExecuteBackupJobCommand { get; }
        public ICommand ExecuteAllBackupJobsCommand { get; }
        public ICommand StartAllBackupJobsCommand { get; }
        public ICommand ListAllBackupJobsCommand { get; }
        public ICommand DeleteBackupJobCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand SaveNewJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand DeleteSelectedJobCommand { get; }
        public ICommand ApplyLanguageCommand { get; }
        public ICommand ApplySettingsCommand { get; }

        public MainWindowViewModel()
        {
            // 1. Récupération du Singleton BackupManager
            _backupManager = BackupManager.GetBM();

            // 2. Initialisation de la liste observable
            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);

            // 2b. Initialiser la collection de progression
            JobsProgress = new ObservableCollection<JobProgressItem>();

            // 3. Initialiser la collection des types de backup
            BackupTypes = new ObservableCollection<string>
            {
                TypeComplete,
                TypeDifferential
            };

            // 4. Initialiser la collection des modes de fenêtre
            WindowModes = new ObservableCollection<string>
            {
                _i18n.GetString("settings_window_windowed"),
                _i18n.GetString("settings_window_maximized"),
                _i18n.GetString("settings_window_fullscreen")
            };

            // Initialize commands
            SwitchThemeCommand = new RelayCommand<string>(SwitchTheme);
            CreateBackupJobCommand = new RelayCommand(CreateBackupJob);
            ExecuteBackupJobCommand = new RelayCommand(ExecuteBackupJob);
            ExecuteAllBackupJobsCommand = new RelayCommand(ExecuteAllBackupJobs);
            StartAllBackupJobsCommand = new RelayCommand(StartAllBackupJobs);
            ListAllBackupJobsCommand = new RelayCommand(ListAllBackupJobs);
            DeleteBackupJobCommand = new RelayCommand(DeleteBackupJob);
            ChangeLanguageCommand = new RelayCommand(ChangeLanguage);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ExitCommand = new RelayCommand(Exit);
            BackToMenuCommand = new RelayCommand(BackToMenu);
            SaveNewJobCommand = new RelayCommand(SaveNewJob);
            ExecuteSelectedJobCommand = new RelayCommand(ExecuteSelectedJob);
            DeleteSelectedJobCommand = new RelayCommand(DeleteSelectedJob);
            ApplyLanguageCommand = new RelayCommand(ApplyLanguage);
            ApplySettingsCommand = new RelayCommand(ApplySettings);
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

        private async void SaveNewJob()
        {
            BackupType type = SelectedBackupType == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = _backupManager.AddJob(NewJobName, NewJobSource, NewJobTarget, type);

            if (success)
            {
                RefreshBackupJobs();
                StatusMessage = "✓ " + (_i18n.GetString("create_success") ?? "Job created successfully!");

                // Attendre 2 secondes pour afficher le message
                await Task.Delay(2000);

                // Réinitialiser le formulaire pour permettre de créer un nouveau job
                NewJobName = "";
                NewJobSource = "";
                NewJobTarget = "";
                SelectedBackupType = 0;
                StatusMessage = "";
            }
            else
            {
                StatusMessage = "✗ " + (_i18n.GetString("create_failure") ?? "Failed to create job. Check inputs or max limit.");
            }
        }

        private void ExecuteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ExecuteJob";
        }

        private async void ExecuteSelectedJob()
        {
            if (SelectedJob != null)
            {
                IsExecuting = true;
                CurrentProgress = 0;
                CurrentFileInfo = "";
                int jobId = SelectedJob.Id;
                string jobName = SelectedJob.Name;
                StatusMessage = $"Executing '{jobName}'...";

                await Task.Run(() =>
                {
                    try
                    {
                        // Exécution dans un thread séparé pour ne pas bloquer l'UI
                        _backupManager.ExecuteJob(jobId, progress =>
                        {
                            // Mise à jour de la progression sur le thread UI
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                CurrentProgress = progress.ProgressPercentage;
                                int filesProcessed = progress.TotalFiles - progress.FilesRemaining;
                                CurrentFileInfo = $"{filesProcessed}/{progress.TotalFiles} files";
                                StatusMessage = $"Running: {progress.ProgressPercentage:F1}% - {filesProcessed}/{progress.TotalFiles}";
                            });
                        });

                        // Mise à jour finale sur le thread UI
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            RefreshBackupJobs();
                            SelectedJob = BackupJobs.FirstOrDefault(j => j.Id == jobId);
                            StatusMessage = $"✓ '{jobName}' completed!";
                            CurrentProgress = 100;
                            IsExecuting = false;
                        });
                    }
                    catch (Exception ex)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            IsExecuting = false;
                            StatusMessage = $"✗ Error: {ex.Message}";
                        });
                    }
                });
            }
            else
            {
                StatusMessage = "Please select a job.";
            }
        }

        private void ExecuteAllBackupJobs()
        {
            // Préparer la liste de progression
            JobsProgress.Clear();
            foreach (var job in BackupJobs)
            {
                JobsProgress.Add(new JobProgressItem 
                { 
                    JobName = job.Name,
                    Status = _i18n.GetString("execute_waiting") ?? "Waiting...",
                    ProgressPercentage = 0
                });
            }

            CurrentView = "ExecuteAll";
            IsExecuting = false;
            StatusMessage = "";
        }

        private async void StartAllBackupJobs()
        {
            IsExecuting = true;
            StatusMessage = "";

            await Task.Run(() =>
            {
                try
                {
                    _backupManager.ExecuteAllJobs(progress =>
                    {
                        // Trouver le job correspondant dans JobsProgress
                        var progressItem = JobsProgress.FirstOrDefault(j => j.JobName == progress.BackupName);
                        if (progressItem != null)
                        {
                            progressItem.ProgressPercentage = progress.ProgressPercentage;
                            int filesCopied = progress.TotalFiles - progress.FilesRemaining;
                            progressItem.Status = $"{progress.ProgressPercentage:F1}% - {filesCopied}/{progress.TotalFiles} files";

                            // Si terminé à 100%
                            if (progress.ProgressPercentage >= 100)
                            {
                                progressItem.IsCompleted = true;
                                progressItem.Status = _i18n.GetString("execute_completed") ?? "Completed!";
                            }
                        }
                    });

                    // Marquer tous comme terminés
                    foreach (var item in JobsProgress.Where(j => !j.IsCompleted && !j.HasError))
                    {
                        item.IsCompleted = true;
                        item.ProgressPercentage = 100;
                        item.Status = _i18n.GetString("execute_completed") ?? "Completed!";
                    }

                    RefreshBackupJobs();
                    StatusMessage = "✓ " + (_i18n.GetString("execute_all_completed") ?? "All jobs completed!");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"✗ Error: {ex.Message}";

                    // Marquer les jobs non terminés comme erreur
                    foreach (var item in JobsProgress.Where(j => !j.IsCompleted))
                    {
                        item.HasError = true;
                        item.Status = "Error";
                    }
                }
                finally
                {
                    IsExecuting = false;
                }
            });
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

        private async void DeleteSelectedJob()
        {
            if (SelectedJob != null)
            {
                string jobName = SelectedJob.Name;
                bool success = _backupManager.DeleteJob(SelectedJob.Id);
                if (success)
                {
                    RefreshBackupJobs();
                    StatusMessage = $"✓ '{jobName}' deleted!";

                    // Attendre 2 secondes pour afficher le message
                    await Task.Delay(2000);

                    // Réinitialiser la sélection et le message
                    SelectedJob = null;
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

        private void OpenSettings()
        {
            // Charger les paramètres actuels
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;

            // Détecter le mode de fenêtre actuel
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    // Si ExtendClientArea est actif, c'est le mode fullscreen sans bordure
                    if (mainWindow.ExtendClientAreaToDecorationsHint)
                    {
                        SelectedWindowMode = 2;
                    }
                    else if (mainWindow.WindowState == WindowState.Maximized)
                    {
                        SelectedWindowMode = 1;
                    }
                    else
                    {
                        SelectedWindowMode = 0;
                    }
                }
            }

            CurrentView = "Settings";
            StatusMessage = "";
        }

        private async void ApplySettings()
        {
            try
            {
                // IMPORTANT: Sauvegarder SelectedWindowMode AVANT de changer la langue
                // car RefreshTranslations() va réinitialiser la ComboBox
                int savedWindowMode = SelectedWindowMode;

                // Appliquer la langue
                var i18n = I18n.Instance;
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);
                RefreshTranslations();

                // Restaurer SelectedWindowMode après le refresh
                SelectedWindowMode = savedWindowMode;

                // Appliquer le mode de fenêtre
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        switch (SelectedWindowMode)
                        {
                            case 0: // Windowed (Normal)
                                mainWindow.ExtendClientAreaToDecorationsHint = false;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Normal;
                                break;

                            case 1: // Maximized (avec bordures)
                                mainWindow.ExtendClientAreaToDecorationsHint = false;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;

                            case 2: // Fullscreen sans bordure
                                mainWindow.ExtendClientAreaToDecorationsHint = true;
                                mainWindow.ExtendClientAreaTitleBarHeightHint = -1;
                                mainWindow.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;
                        }
                    }
                }

                // Afficher message de confirmation
                StatusMessage = "✓ " + i18n.GetString("settings_applied");
                await Task.Delay(2000);
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
        }

        private void ApplyLanguage()
        {
            try
            {
                // Récupérer l'instance I18n
                var i18n = I18n.Instance;

                // Appliquer la langue sélectionnée
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);

                // Notifier toutes les propriétés de traduction pour rafraîchir l'interface
                RefreshTranslations();

                // Afficher le message dans la langue nouvellement sélectionnée
                StatusMessage = i18n.GetString("language_changed");

                // Attendre un peu pour que l'utilisateur voie le message
                System.Threading.Thread.Sleep(1000);

                CurrentView = "Menu";
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error changing language: {ex.Message}";
            }
        }

        private void RefreshTranslations()
        {
            // L'objet I18n notifie maintenant automatiquement ses propres changements (Item[])
            // On notifie quand même L au cas où certains bindings en auraient besoin
            OnPropertyChanged(nameof(L));

            // Mettre à jour la collection BackupTypes avec les nouvelles traductions
            BackupTypes.Clear();
            BackupTypes.Add(TypeComplete);
            BackupTypes.Add(TypeDifferential);

            // Mettre à jour la collection WindowModes avec les nouvelles traductions
            WindowModes.Clear();
            WindowModes.Add(_i18n.GetString("settings_window_windowed"));
            WindowModes.Add(_i18n.GetString("settings_window_maximized"));
            WindowModes.Add(_i18n.GetString("settings_window_fullscreen"));

            // Notifier les propriétés individuelles qui sont encore utilisées
            OnPropertyChanged(nameof(TypeComplete));
            OnPropertyChanged(nameof(TypeDifferential));
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