using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
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
    public class JobProgressItem : ObservableObject
    {
        // --- V3.0 Properties ---
        public int JobId { get; set; }

        private string _progressBytes = "";
        public string ProgressBytes { get => _progressBytes; set => SetProperty(ref _progressBytes, value); }

        private bool _isPaused = false;
        public bool IsPaused { get => _isPaused; set => SetProperty(ref _isPaused, value); }

        // --- Shared/Feature Properties ---
        private string _jobName = "";
        public string JobName { get => _jobName; set => SetProperty(ref _jobName, value); }

        private double _progressPercentage = 0;
        public double ProgressPercentage { get => _progressPercentage; set => SetProperty(ref _progressPercentage, value); }

        private string _status = "Waiting...";
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        private bool _isCompleted = false;
        public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }

        private bool _hasError = false;
        public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }
    }

    public class MainWindowViewModel : ObservableObject
    {
        public I18n L => I18n.Instance;
        private readonly BackupManager _backupManager;

        public ObservableCollection<BackupJob> BackupJobs { get; set; }
        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }

        // --- NAVIGATION ---
        private string _currentView = "Menu";
        public string CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

        // --- FORM PROPERTIES ---
        private string _newJobName = "";
        public string NewJobName { get => _newJobName; set => SetProperty(ref _newJobName, value); }

        private string _newJobSource = "";
        public string NewJobSource { get => _newJobSource; set => SetProperty(ref _newJobSource, value); }

        private string _newJobTarget = "";
        public string NewJobTarget { get => _newJobTarget; set => SetProperty(ref _newJobTarget, value); }

        private int _selectedBackupType = 0;
        public int SelectedBackupType { get => _selectedBackupType; set => SetProperty(ref _selectedBackupType, value); }

        private BackupJob? _selectedJob;
        public BackupJob? SelectedJob { get => _selectedJob; set => SetProperty(ref _selectedJob, value); }

        // --- EXECUTION PROPERTIES ---
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isExecuting = false;
        public bool IsExecuting { get => _isExecuting; set => SetProperty(ref _isExecuting, value); }

        private double _currentProgress = 0;
        public double CurrentProgress { get => _currentProgress; set => SetProperty(ref _currentProgress, value); }

        private string _currentFileInfo = "";
        public string CurrentFileInfo { get => _currentFileInfo; set => SetProperty(ref _currentFileInfo, value); }

        // V3.0 Execution Extras
        private string _currentSizeInfo = "";
        public string CurrentSizeInfo { get => _currentSizeInfo; set => SetProperty(ref _currentSizeInfo, value); }

        private bool _isSingleJobPaused = false;
        public bool IsSingleJobPaused { get => _isSingleJobPaused; set => SetProperty(ref _isSingleJobPaused, value); }


        // --- SETTINGS PROPERTIES (Merged Feature + V3) ---
        private int _selectedLanguage = 0;
        public int SelectedLanguage { get => _selectedLanguage; set => SetProperty(ref _selectedLanguage, value); }

        private int _selectedWindowMode = 1;
        public int SelectedWindowMode { get => _selectedWindowMode; set => SetProperty(ref _selectedWindowMode, value); }

        private string _configBusinessSoftware = "";
        public string ConfigBusinessSoftware { get => _configBusinessSoftware; set => SetProperty(ref _configBusinessSoftware, value); }

        private string _configCryptoPath = "";
        public string ConfigCryptoPath { get => _configCryptoPath; set => SetProperty(ref _configCryptoPath, value); }

        private string _configCryptoKey = "";
        public string ConfigCryptoKey { get => _configCryptoKey; set => SetProperty(ref _configCryptoKey, value); }

        private long _configMaxSize = 1000;
        public long ConfigMaxSize { get => _configMaxSize; set => SetProperty(ref _configMaxSize, value); }

        private string _configPriorityExtensions = "";
        public string ConfigPriorityExtensions { get => _configPriorityExtensions; set => SetProperty(ref _configPriorityExtensions, value); }

        private int _selectedLogFormat = 0;
        public int SelectedLogFormat { get => _selectedLogFormat; set => SetProperty(ref _selectedLogFormat, value); }

        public ObservableCollection<string> LogFormats { get; } = new() { "JSON", "XML" };
        public ObservableCollection<string> BackupTypes { get; private set; }
        public ObservableCollection<string> WindowModes { get; private set; }

        // --- DYNAMIC TRANSLATIONS (Properties from Feature Branch) ---
        private I18n _i18n = I18n.Instance;

        public string AppTitle => _i18n.GetString("app_title");
        public string MenuTitle => _i18n.GetString("menu_title");
        public string MenuDashboard => _i18n.GetString("menu_dashboard");
        public string MenuActionsTitle => _i18n.GetString("menu_actions_title");
        public string MenuCreate => _i18n.GetString("menu_create");
        public string MenuExecute => _i18n.GetString("menu_execute");
        public string MenuExecuteAll => _i18n.GetString("menu_execute_all");
        public string MenuList => _i18n.GetString("menu_list");
        public string MenuDelete => _i18n.GetString("menu_delete");
        public string MenuSettings => _i18n.GetString("menu_settings");
        public string MenuExit => _i18n.GetString("menu_exit");
        
        public string ThemeDark => _i18n.GetString("theme_dark");
        public string ThemeLight => _i18n.GetString("theme_light");
        
        public string DashboardWelcome => _i18n.GetString("dashboard_welcome");
        public string DashboardSubtitle => _i18n.GetString("dashboard_subtitle");
        
        public string CreateTitle => _i18n.GetString("create_title");
        public string CreateName => _i18n.GetString("create_name");
        public string CreateSource => _i18n.GetString("create_source");
        public string CreateTarget => _i18n.GetString("create_target");
        public string CreateType => _i18n.GetString("create_type");
        public string CreateButton => _i18n.GetString("create_button");
        public string BtnSave => _i18n.GetString("btn_save");
        public string BtnCancel => _i18n.GetString("btn_cancel");

        public string ExecuteTitle => _i18n.GetString("execute_title");
        public string ExecuteSubtitle => _i18n.GetString("execute_subtitle");
        public string ExecuteSelect => _i18n.GetString("execute_select");
        public string ExecutePlaceholder => _i18n.GetString("execute_placeholder");
        public string ExecuteButton => _i18n.GetString("execute_button");
        public string ExecuteNoJobs => _i18n.GetString("execute_no_jobs");
        public string BtnStart => _i18n.GetString("btn_start");

        public string ExecuteAllTitle => _i18n.GetString("execute_all_title");
        public string ExecuteAllSubtitle => _i18n.GetString("execute_all_subtitle");
        public string BtnStartAll => _i18n.GetString("btn_start_all");

        public string ListTitle => _i18n.GetString("list_title");
        public string ListId => _i18n.GetString("list_id");
        public string ListName => _i18n.GetString("list_name");
        public string ListSource => _i18n.GetString("list_source");
        public string ListTarget => _i18n.GetString("list_target");

        public string DeleteTitle => _i18n.GetString("delete_title");
        public string DeleteWarning => _i18n.GetString("delete_warning");
        public string DeleteNoJobs => _i18n.GetString("delete_no_jobs");
        public string BtnDelete => _i18n.GetString("btn_delete");

        // Settings Strings
        public string SettingsTitle => _i18n.GetString("settings_title");
        public string SettingsLang => _i18n.GetString("settings_lang");
        public string SettingsWindowMode => _i18n.GetString("settings_window_mode");
        public string SettingsGeneral => _i18n.GetString("settings_general");
        public string SettingsBusinessSoft => _i18n.GetString("settings_business_soft");
        public string SettingsLogFormat => _i18n.GetString("settings_log_format");
        public string SettingsCrypto => _i18n.GetString("settings_crypto");
        public string SettingsCryptoPath => _i18n.GetString("settings_crypto_path");
        public string SettingsCryptoKey => _i18n.GetString("settings_crypto_key");
        public string SettingsPerformance => _i18n.GetString("settings_performance");
        public string SettingsMaxSize => _i18n.GetString("settings_max_size");
        public string SettingsPriorityExt => _i18n.GetString("settings_priority_ext");
        public string BtnApply => _i18n.GetString("btn_apply");

        public string LanguageTitle => _i18n.GetString("language_title");
        public string LanguageSelect => _i18n.GetString("language_select");
        public string LanguageApply => _i18n.GetString("language_apply");
        public string ButtonBack => _i18n.GetString("button_back");

        public string TypeComplete => _i18n.GetString("type_complete");
        public string TypeDifferential => _i18n.GetString("type_differential");


        // --- COMMANDS ---
        public ICommand SwitchThemeCommand { get; }
        public ICommand CreateBackupJobCommand { get; }
        public ICommand ExecuteBackupJobCommand { get; }
        public ICommand ExecuteAllBackupJobsCommand { get; }
        public ICommand StartAllBackupJobsCommand { get; }
        public ICommand ListAllBackupJobsCommand { get; }
        public ICommand DeleteBackupJobCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand SaveNewJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand DeleteSelectedJobCommand { get; }
        public ICommand ApplySettingsCommand { get; }

        // V3.0 Control Commands
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand PauseAllCommand { get; }
        public ICommand ResumeAllCommand { get; }
        public ICommand StopAllCommand { get; }
        public ICommand TogglePauseJobCommand { get; }

        public MainWindowViewModel()
        {
            _backupManager = BackupManager.GetBM();

            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);
            JobsProgress = new ObservableCollection<JobProgressItem>();

            // Initialize Collections
            BackupTypes = new ObservableCollection<string> { TypeComplete, TypeDifferential };
            WindowModes = new ObservableCollection<string>
            {
                _i18n.GetString("settings_window_windowed"),
                _i18n.GetString("settings_window_maximized"),
                _i18n.GetString("settings_window_fullscreen")
            };

            // Standard Commands
            SwitchThemeCommand = new RelayCommand<string>(SwitchTheme);
            CreateBackupJobCommand = new RelayCommand(() => { CurrentView = "CreateJob"; StatusMessage = ""; });
            ExecuteBackupJobCommand = new RelayCommand(() => { RefreshBackupJobs(); CurrentView = "ExecuteJob"; StatusMessage = ""; });
            ExecuteAllBackupJobsCommand = new RelayCommand(ExecuteAllBackupJobs);
            StartAllBackupJobsCommand = new RelayCommand(StartAllBackupJobs);
            ListAllBackupJobsCommand = new RelayCommand(() => { RefreshBackupJobs(); CurrentView = "ListJobs"; StatusMessage = ""; });
            DeleteBackupJobCommand = new RelayCommand(() => { RefreshBackupJobs(); CurrentView = "DeleteJob"; StatusMessage = ""; });
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ExitCommand = new RelayCommand(Exit);
            BackToMenuCommand = new RelayCommand(() => { CurrentView = "Menu"; StatusMessage = ""; });
            SaveNewJobCommand = new RelayCommand(SaveNewJob);
            ExecuteSelectedJobCommand = new RelayCommand(ExecuteSelectedJob);
            DeleteSelectedJobCommand = new RelayCommand(DeleteSelectedJob);
            ApplySettingsCommand = new RelayCommand(ApplySettings);

            // V3.0 Control Commands
            PauseJobCommand = new RelayCommand<int>(id => _backupManager.PauseJob(id));
            ResumeJobCommand = new RelayCommand<int>(id => _backupManager.ResumeJob(id));
            StopJobCommand = new RelayCommand<int>(id => _backupManager.StopJob(id));
            PauseAllCommand = new RelayCommand(() => _backupManager.PauseAllJobs());
            ResumeAllCommand = new RelayCommand(() => _backupManager.ResumeAllJobs());
            StopAllCommand = new RelayCommand(() => _backupManager.StopAllJobs());
            TogglePauseJobCommand = new RelayCommand<int>(TogglePauseJob);

            LoadPersistentSettings();
        }

        private void LoadPersistentSettings()
        {
            var config = _backupManager.ConfigManager;
            string savedLang = config.GetConfig<string>("AppLanguage") ?? "en_us";

            try
            {
                I18n.Instance.SetLanguage(savedLang);
            }
            catch (ArgumentException)
            {
                I18n.Instance.SetLanguage("en_us");
                config.SetConfig("AppLanguage", "en_us");
                config.SaveConfiguration();
                savedLang = "en_us";
            }

            SelectedLanguage = savedLang == "fr_fr" ? 1 : 0;
            RefreshTranslations();

            SelectedWindowMode = config.GetConfig<int>("AppWindowMode");

            Dispatcher.UIThread.Post(async () => {
                await ApplyWindowModeLogic(SelectedWindowMode);
            }, DispatcherPriority.Loaded);
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

        private async Task ApplyWindowModeLogic(int mode)
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    switch (mode)
                    {
                        case 0: // Windowed
                            mainWindow.ExtendClientAreaToDecorationsHint = false;
                            mainWindow.WindowState = WindowState.Normal;
                            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            break;
                        case 1: // Maximized
                            mainWindow.ExtendClientAreaToDecorationsHint = false;
                            await Task.Delay(50);
                            mainWindow.WindowState = WindowState.Maximized;
                            break;
                        case 2: // Fullscreen
                            mainWindow.ExtendClientAreaToDecorationsHint = true;
                            mainWindow.ExtendClientAreaTitleBarHeightHint = -1;
                            mainWindow.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
                            await Task.Delay(50);
                            mainWindow.WindowState = WindowState.Maximized;
                            break;
                    }
                }
            }
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
                                // --- V3.0 Logic: Byte Calculation ---
                                long processedBytes = progress.TotalSize - progress.SizeRemaining;

                                // Safety check for division by zero
                                if (progress.TotalSize > 0)
                                {
                                    CurrentProgress = (double)processedBytes / progress.TotalSize * 100;
                                }
                                else
                                {
                                    CurrentProgress = 0;
                                }

                                string processedStr = FormatBytes(processedBytes);
                                string totalStr = FormatBytes(progress.TotalSize);
                                CurrentSizeInfo = $"{processedStr} / {totalStr}";

                                // Pause State Management
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
                    JobId = job.Id,
                    JobName = job.Name,
                    Status = _i18n.GetString("execute_waiting") ?? "Waiting...",
                    ProgressPercentage = 0
                });
            }

            CurrentView = "ExecuteAll";
            IsExecuting = false;
            StatusMessage = "";
        }

        private bool _allPaused = false;

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
                                long processedBytes = progress.TotalSize - progress.SizeRemaining;

                                if (progress.TotalSize > 0)
                                    item.ProgressPercentage = (double)processedBytes / progress.TotalSize * 100;
                                else
                                    item.ProgressPercentage = 0;

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

        private void OpenSettings()
        {
            var config = _backupManager.ConfigManager;
            
            // Load detailed settings (Feature Branch)
            ConfigBusinessSoftware = config.GetConfig<string>("BusinessSoftware") ?? "";
            ConfigCryptoPath = config.GetConfig<string>("CryptoSoftPath") ?? "";
            ConfigCryptoKey = config.GetConfig<string>("CryptoKey") ?? "";
            ConfigMaxSize = config.GetConfig<long?>("MaxParallelTransferSize") ?? 1000;

            var extensions = config.GetConfig<List<string>>("PriorityExtensions") ?? new List<string>();
            ConfigPriorityExtensions = string.Join(", ", extensions);

            string logFormat = config.GetConfig<string>("LoggerFormat") ?? "json";
            SelectedLogFormat = logFormat.ToLower() == "xml" ? 1 : 0;

            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;

            // Load Window Mode (V3 Logic)
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

            CurrentView = "Settings";
            StatusMessage = "";
        }

        private async void ApplySettings()
        {
            try
            {
                int modeToApply = SelectedWindowMode;
                var i18n = I18n.Instance;

                // 1. Language
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);

                RefreshTranslations();

                // 2. Window Mode
                SelectedWindowMode = modeToApply;
                await ApplyWindowModeLogic(SelectedWindowMode);

                // 3. Save Configuration
                var config = _backupManager.ConfigManager;
                config.SetConfig("AppLanguage", languageCode);
                config.SetConfig("AppWindowMode", SelectedWindowMode);
                config.SetConfig("BusinessSoftware", ConfigBusinessSoftware);
                config.SetConfig("CryptoSoftPath", ConfigCryptoPath);
                config.SetConfig("CryptoKey", ConfigCryptoKey);
                config.SetConfig("MaxParallelTransferSize", ConfigMaxSize);
                config.SetConfig("LoggerFormat", SelectedLogFormat == 1 ? "xml" : "json");

                var extList = ConfigPriorityExtensions.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)).ToList();
                config.SetConfig("PriorityExtensions", extList);

                config.SaveConfiguration();

                StatusMessage = "✓ " + i18n.GetString("settings_applied");
                await Task.Delay(2000);
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
        }

        private void RefreshTranslations()
        {
            // Rafraichit toutes les propriétés liées aux textes
            OnPropertyChanged(nameof(AppTitle));
            OnPropertyChanged(nameof(MenuDashboard));
            OnPropertyChanged(nameof(MenuActionsTitle));
            OnPropertyChanged(nameof(MenuCreate));
            OnPropertyChanged(nameof(MenuExecute));
            OnPropertyChanged(nameof(MenuExecuteAll));
            OnPropertyChanged(nameof(MenuList));
            OnPropertyChanged(nameof(MenuDelete));
            OnPropertyChanged(nameof(MenuSettings));
            OnPropertyChanged(nameof(MenuExit));
            OnPropertyChanged(nameof(ThemeDark));
            OnPropertyChanged(nameof(ThemeLight));
            OnPropertyChanged(nameof(DashboardWelcome));
            OnPropertyChanged(nameof(DashboardSubtitle));
            OnPropertyChanged(nameof(CreateTitle));
            OnPropertyChanged(nameof(CreateName));
            OnPropertyChanged(nameof(CreateSource));
            OnPropertyChanged(nameof(CreateTarget));
            OnPropertyChanged(nameof(CreateType));
            OnPropertyChanged(nameof(BtnSave));
            OnPropertyChanged(nameof(BtnCancel));
            OnPropertyChanged(nameof(ExecuteTitle));
            OnPropertyChanged(nameof(ExecuteSubtitle));
            OnPropertyChanged(nameof(ExecutePlaceholder));
            OnPropertyChanged(nameof(BtnStart));
            OnPropertyChanged(nameof(ExecuteAllTitle));
            OnPropertyChanged(nameof(ExecuteAllSubtitle));
            OnPropertyChanged(nameof(BtnStartAll));
            OnPropertyChanged(nameof(ListTitle));
            OnPropertyChanged(nameof(DeleteTitle));
            OnPropertyChanged(nameof(DeleteWarning));
            OnPropertyChanged(nameof(BtnDelete));

            // Paramètres
            OnPropertyChanged(nameof(SettingsTitle));
            OnPropertyChanged(nameof(SettingsLang));
            OnPropertyChanged(nameof(SettingsWindowMode));
            OnPropertyChanged(nameof(SettingsGeneral));
            OnPropertyChanged(nameof(SettingsBusinessSoft));
            OnPropertyChanged(nameof(SettingsLogFormat));
            OnPropertyChanged(nameof(SettingsCrypto));
            OnPropertyChanged(nameof(SettingsCryptoPath));
            OnPropertyChanged(nameof(SettingsCryptoKey));
            OnPropertyChanged(nameof(SettingsPerformance));
            OnPropertyChanged(nameof(SettingsMaxSize));
            OnPropertyChanged(nameof(SettingsPriorityExt));
            OnPropertyChanged(nameof(BtnApply));

            OnPropertyChanged(nameof(TypeComplete));
            OnPropertyChanged(nameof(TypeDifferential));

            // Listes
            BackupTypes.Clear();
            BackupTypes.Add(_i18n.GetString("type_complete"));
            BackupTypes.Add(_i18n.GetString("type_differential"));

            WindowModes.Clear();
            WindowModes.Add(_i18n.GetString("settings_window_windowed"));
            WindowModes.Add(_i18n.GetString("settings_window_maximized"));
            WindowModes.Add(_i18n.GetString("settings_window_fullscreen"));
        }

        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) 
                desktop.Shutdown();
        }

        // --- HELPER METHODS FROM V3.0 ---

        private void TogglePauseJob(int id)
        {
            var job = _backupManager.GetAllJobs().FirstOrDefault(j => j.Id == id);
            if (job != null)
            {
                if (job.State == State.Paused)
                {
                    _backupManager.ResumeJob(id);
                    UpdateUiState(id, false, _i18n.GetString("status_running") ?? "Running");
                }
                else
                {
                    _backupManager.PauseJob(id);
                    UpdateUiState(id, true, _i18n.GetString("status_paused_user") ?? "Paused by user");
                }
            }
        }

        private void UpdateUiState(int jobId, bool isPaused, string message)
        {
            // 1. Update Single View (ExecuteJob)
            if (SelectedJob?.Id == jobId)
            {
                IsSingleJobPaused = isPaused;
                StatusMessage = message;
            }

            // 2. Update List View (ExecuteAll)
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