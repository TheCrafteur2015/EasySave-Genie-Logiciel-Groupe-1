using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using EasySave.View.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyGUI.ViewModels
{
    /// <summary>
    /// Represents the progress status of a specific backup job.
    /// Used to track and display real-time updates in the UI for individual jobs.
    /// </summary>
    public class JobProgressItem : ObservableObject
    {
        // V3.0 : ID nécessaire pour les commandes Pause/Stop individuelles
        public int JobId { get; set; }

        private string _jobName = "";
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        private string _progressBytes = "";
        public string ProgressBytes
        {
            get => _progressBytes;
            set => SetProperty(ref _progressBytes, value);
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

        private bool _isPaused = false;
        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }
    }

    public class MainWindowViewModel : ObservableObject
    {
        // --- DYNAMIC TRANSLATION MANAGEMENT ---
        public I18n L => I18n.Instance;
        public string Greeting { get; } = "Welcome to EasySave!";

        private readonly BackupManager _backupManager;

        public ObservableCollection<BackupJob> BackupJobs { get; set; }

        // Navigation
        private string _currentView = "Menu";
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // --- FORM PROPERTIES ---
        private string _newJobName = "";
        public string NewJobName
        {
            get => _newJobName;
            set => SetProperty(ref _newJobName, value);
        }

        private string _currentSizeInfo = "";
        public string CurrentSizeInfo
        {
            get => _currentSizeInfo;
            set => SetProperty(ref _currentSizeInfo, value);
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

        // --- SETTINGS PROPERTIES (COMPLET V1.1, V2.0, V3.0) ---
        private int _selectedLanguage = 0;
        public int SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        private int _selectedWindowMode = 1;
        public int SelectedWindowMode
        {
            get => _selectedWindowMode;
            set => SetProperty(ref _selectedWindowMode, value);
        }

        // V1.1 : Format Log (JSON/XML)
        private int _selectedLogFormat = 0; // 0=JSON, 1=XML
        public int SelectedLogFormat
        {
            get => _selectedLogFormat;
            set => SetProperty(ref _selectedLogFormat, value);
        }

        // V2.0/V3.0 : Logiciel Métier
        private string _businessSoftware = "";
        public string BusinessSoftware
        {
            get => _businessSoftware;
            set => SetProperty(ref _businessSoftware, value);
        }

        // V2.0/V3.0 : Extensions Prioritaires / Cryptées
        private string _priorityExtensions = "";
        public string PriorityExtensions
        {
            get => _priorityExtensions;
            set => SetProperty(ref _priorityExtensions, value);
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

        private bool _isSingleJobPaused = false;
        public bool IsSingleJobPaused
        {
            get => _isSingleJobPaused;
            set => SetProperty(ref _isSingleJobPaused, value);
        }

        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }

        private I18n _i18n = I18n.Instance;

        // Localized Strings
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
        public ICommand TogglePauseJobCommand { get; }

        public ObservableCollection<string> BackupTypes { get; private set; }
        public ObservableCollection<string> WindowModes { get; private set; }

        // --- COMMANDS ---
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

        // V3.0 : Commandes de contrôle Temps Réel
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand PauseAllCommand { get; }
        public ICommand ResumeAllCommand { get; }
        public ICommand StopAllCommand { get; }

        public MainWindowViewModel()
        {
            _backupManager = BackupManager.GetBM();

            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);
            JobsProgress = new ObservableCollection<JobProgressItem>();

            BackupTypes = new ObservableCollection<string> { TypeComplete, TypeDifferential };
            WindowModes = new ObservableCollection<string>
            {
                _i18n.GetString("settings_window_windowed"),
                _i18n.GetString("settings_window_maximized"),
                _i18n.GetString("settings_window_fullscreen")
            };



            // Standard Commands
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

            // V3.0 Control Commands
            // Ces commandes appellent directement le BackupManager qui gère les primitives de synchro (Mutex, Events)
            PauseJobCommand = new RelayCommand<int>(id => _backupManager.PauseJob(id));
            ResumeJobCommand = new RelayCommand<int>(id => _backupManager.ResumeJob(id));
            StopJobCommand = new RelayCommand<int>(id => _backupManager.StopJob(id));

            PauseAllCommand = new RelayCommand(() => _backupManager.PauseAllJobs());
            ResumeAllCommand = new RelayCommand(() => _backupManager.ResumeAllJobs());
            StopAllCommand = new RelayCommand(() => _backupManager.StopAllJobs());
            TogglePauseJobCommand = new RelayCommand<int>(TogglePauseJob);
        }

        // --- METHODS ---

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
            if (_backupManager.GetAllJobs().Any(j => j.Name.Equals(NewJobName, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "✗ " + _i18n.GetString("create_error_duplicate");
                return;
            }

            BackupType type = SelectedBackupType == 0 ? BackupType.Complete : BackupType.Differential;

            bool success = _backupManager.AddJob(NewJobName, NewJobSource, NewJobTarget, type);

            if (success)
            {
                RefreshBackupJobs();
                StatusMessage = "✓ " + (_i18n.GetString("create_success") ?? "Job created successfully!");
                await Task.Delay(2000);
                NewJobName = "";
                NewJobSource = "";
                NewJobTarget = "";
                SelectedBackupType = 0;
                StatusMessage = "";
            }
            else
            {
                StatusMessage = "✗ " + (_i18n.GetString("create_failure") ?? "Failed to create job.");
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
                CurrentSizeInfo = "Calculating...";
                IsSingleJobPaused = false;
                int jobId = SelectedJob.Id;
                string jobName = SelectedJob.Name;
                StatusMessage = $"Executing '{jobName}'...";

                await Task.Run(() =>
                {
                    try
                    {
                        _backupManager.ExecuteJob(jobId, progress =>
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                // --- MODIFICATION : CALCUL DU POURCENTAGE EN OCTETS ---
                                long processedBytes = progress.TotalSize - progress.SizeRemaining;

                                // Sécurité division par zéro
                                if (progress.TotalSize > 0)
                                {
                                    CurrentProgress = (double)processedBytes / progress.TotalSize * 100;
                                }
                                else
                                {
                                    CurrentProgress = 0;
                                }
                                // -----------------------------------------------------

                                string processedStr = FormatBytes(processedBytes);
                                string totalStr = FormatBytes(progress.TotalSize);
                                CurrentSizeInfo = $"{processedStr} / {totalStr}";

                                // Gestion de l'état Pause pour le bouton
                                if (progress.State == State.Paused) IsSingleJobPaused = true;
                                else if (progress.State == State.Active) IsSingleJobPaused = false;

                                if (!string.IsNullOrEmpty(progress.Message))
                                {
                                    if (progress.State == State.Paused) StatusMessage = "⏸️ " + progress.Message;
                                    else if (progress.State == State.Error) StatusMessage = "✗ " + progress.Message;
                                    else StatusMessage = progress.Message;
                                }
                                else
                                {
                                    StatusMessage = $"Running: {CurrentProgress:F1}%";
                                }
                            });
                        });

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            RefreshBackupJobs();
                            SelectedJob = BackupJobs.FirstOrDefault(j => j.Id == jobId);
                            if (SelectedJob?.State == State.Completed)
                            {
                                StatusMessage = $"✓ '{jobName}' completed!";
                                CurrentProgress = 100;
                            }
                            else if (SelectedJob?.State == State.Error)
                            {
                                StatusMessage = $"✗ '{jobName}' stopped or error.";
                            }
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
            JobsProgress.Clear();
            foreach (var job in BackupJobs)
            {
                JobsProgress.Add(new JobProgressItem
                {
                    JobId = job.Id, // V3.0: Important pour le binding des commandes individuelles
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
            _allPaused = false;

            await Task.Run(() =>
            {
                try
                {
                    _backupManager.ExecuteAllJobs(progress =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var item = JobsProgress.FirstOrDefault(j => j.JobName == progress.BackupName);
                            if (item != null)
                            {
                                // --- MODIFICATION : CALCUL DU POURCENTAGE EN OCTETS ---
                                long processedBytes = progress.TotalSize - progress.SizeRemaining;

                                if (progress.TotalSize > 0)
                                    item.ProgressPercentage = (double)processedBytes / progress.TotalSize * 100;
                                else
                                    item.ProgressPercentage = 0;
                                // -----------------------------------------------------

                                string processedStr = FormatBytes(processedBytes);
                                string totalStr = FormatBytes(progress.TotalSize);
                                item.ProgressBytes = $"{processedStr} / {totalStr}";

                                if (progress.State == State.Paused) item.IsPaused = true;
                                else if (progress.State == State.Active) item.IsPaused = false;

                                if (!string.IsNullOrEmpty(progress.Message))
                                {
                                    item.Status = progress.Message;
                                    if (progress.State == State.Paused) item.Status = "⏸️ " + progress.Message;
                                    else if (progress.State == State.Error) { item.HasError = true; item.Status = "✗ " + progress.Message; }
                                }
                                else
                                {
                                    item.Status = $"{item.ProgressPercentage:F1}%";
                                }

                                if (item.ProgressPercentage >= 100 && progress.State == State.Completed)
                                {
                                    item.IsCompleted = true;
                                    item.Status = "Completed!";
                                }
                            }
                        });
                    });

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var item in JobsProgress.Where(j => !j.IsCompleted && !j.HasError))
                        {
                            item.IsCompleted = true;
                            item.ProgressPercentage = 100;
                            item.Status = "Completed!";
                        }
                        RefreshBackupJobs();
                        StatusMessage = "✓ All jobs completed!";
                    });
                }
                catch (Exception ex)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        StatusMessage = $"✗ Error: {ex.Message}";
                        foreach (var item in JobsProgress.Where(j => !j.IsCompleted))
                        {
                            item.HasError = true;
                            item.Status = "Error";
                        }
                    });
                }
                finally
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => IsExecuting = false);
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
                    await Task.Delay(2000);
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
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;
            CurrentView = "ChangeLanguage";
            StatusMessage = "";
        }

        // V3.0 : Mise à jour pour charger tous les réglages
        private void OpenSettings()
        {
            // Langue et WindowMode (Existant)
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.ExtendClientAreaToDecorationsHint) SelectedWindowMode = 2;
                    else if (mainWindow.WindowState == WindowState.Maximized) SelectedWindowMode = 1;
                    else SelectedWindowMode = 0;
                }
            }

            // Chargement des Nouveaux paramètres V1.1 / V2.0 / V3.0
            // Logger
            string currentFormat = _backupManager.ConfigManager.GetConfig<string>("LoggerFormat");
            SelectedLogFormat = currentFormat == "xml" ? 1 : 0;

            // Business Software
            BusinessSoftware = _backupManager.ConfigManager.GetConfig<string>("BusinessSoftware") ?? "";

            // Extensions (List -> String pour affichage)
            var extensions = _backupManager.ConfigManager.GetConfig<List<string>>("PriorityExtensions");
            PriorityExtensions = extensions != null ? string.Join(", ", extensions) : "";

            CurrentView = "Settings";
            StatusMessage = "";
        }

        // V3.0 : Mise à jour pour sauvegarder tous les réglages
        private async void ApplySettings()
        {
            try
            {
                int savedWindowMode = SelectedWindowMode;
                var i18n = I18n.Instance;

                // 1. Langue
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);
                RefreshTranslations();

                // 2. Window Mode
                SelectedWindowMode = savedWindowMode;
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow != null)
                    {
                        switch (SelectedWindowMode)
                        {
                            case 0:
                                mainWindow.ExtendClientAreaToDecorationsHint = false;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Normal;
                                break;
                            case 1:
                                mainWindow.ExtendClientAreaToDecorationsHint = false;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;
                            case 2:
                                mainWindow.ExtendClientAreaToDecorationsHint = true;
                                mainWindow.ExtendClientAreaTitleBarHeightHint = -1;
                                mainWindow.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;
                        }
                    }
                }

                // 3. Format Log (V1.1)
                string newFormat = SelectedLogFormat == 1 ? "xml" : "json";
                _backupManager.ConfigManager.SetConfig("LoggerFormat", newFormat);

                // 4. Logiciel Métier (V2.0/V3.0)
                _backupManager.ConfigManager.SetConfig("BusinessSoftware", BusinessSoftware);

                // 5. Extensions (V2.0/V3.0)
                var extList = PriorityExtensions.Split(',')
                                                .Select(e => e.Trim())
                                                .Where(e => !string.IsNullOrEmpty(e))
                                                .ToList();
                _backupManager.ConfigManager.SetConfig("PriorityExtensions", extList);

                // Sauvegarde disque
                _backupManager.ConfigManager.SaveConfiguration();

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
                var i18n = I18n.Instance;
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);
                RefreshTranslations();
                StatusMessage = i18n.GetString("language_changed");
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
            OnPropertyChanged(nameof(L));
            BackupTypes.Clear();
            BackupTypes.Add(TypeComplete);
            BackupTypes.Add(TypeDifferential);
            WindowModes.Clear();
            WindowModes.Add(_i18n.GetString("settings_window_windowed"));
            WindowModes.Add(_i18n.GetString("settings_window_maximized"));
            WindowModes.Add(_i18n.GetString("settings_window_fullscreen"));
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

        // --- NOUVELLES MÉTHODES POUR LE BOUTON PLAY/PAUSE ---

        private void TogglePauseJob(int id)
        {
            var job = _backupManager.GetAllJobs().FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                if (job.State == State.Paused)
                {
                    _backupManager.ResumeJob(id);
                    // On force l'interface à afficher "Running" tout de suite
                    UpdateUiState(id, false, _i18n.GetString("status_running"));
                }
                else
                {
                    _backupManager.PauseJob(id);
                    // On force l'interface à afficher "Paused" tout de suite
                    UpdateUiState(id, true, _i18n.GetString("status_paused_user"));
                }
            }
        }

        private bool _allPaused = false;
        private void TogglePauseAll()
        {
            if (_allPaused)
            {
                _backupManager.ResumeAllJobs();
                _allPaused = false;
                foreach (var item in JobsProgress)
                {
                    item.IsPaused = false;
                    item.Status = _i18n.GetString("status_running");
                }
            }
            else
            {
                _backupManager.PauseAllJobs();
                _allPaused = true;
                foreach (var item in JobsProgress)
                {
                    item.IsPaused = true;
                    item.Status = _i18n.GetString("status_paused_all");
                }
            }
        }

        // Cette méthode sert à tricher : on met à jour l'icône AVANT que le thread ne réponde
        private void UpdateUiState(int jobId, bool isPaused, string message)
        {
            // 1. Mise à jour Vue Unique (ExecuteJob)
            if (SelectedJob?.Id == jobId)
            {
                IsSingleJobPaused = isPaused;
                StatusMessage = message;
            }

            // 2. Mise à jour Vue Liste (ExecuteAll)
            var item = JobsProgress.FirstOrDefault(j => j.JobId == jobId);
            if (item != null)
            {
                item.IsPaused = isPaused;
                item.Status = message;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n2} {1}", number, suffixes[counter]);
        }
    }
}