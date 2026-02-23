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
    /// <summary>
    /// Represents the progress status of a specific backup job.
    /// Used to track and display real-time updates in the UI for individual jobs.
    /// This class inherits from ObservableObject to support data binding in the GUI.
    /// </summary>
    public class JobProgressItem : ObservableObject
    {
        // --- V3.0 Properties ---
        /// <summary>
        /// Gets or sets the unique identifier of the backup job.
        /// Required for individual control commands such as Pause, Resume, or Stop.
        /// </summary>
        public int JobId { get; set; }

        private string _progressBytes = "";
        /// <summary>
        /// Gets or sets the formatted string representing the progress in bytes (e.g., "50MB / 100MB").
        /// </summary>
        public string ProgressBytes { get => _progressBytes; set => SetProperty(ref _progressBytes, value); }

        private bool _isPaused = false;
        /// <summary>
        /// Gets or sets a value indicating whether the backup job is currently paused.
        /// </summary>
        public bool IsPaused { get => _isPaused; set => SetProperty(ref _isPaused, value); }

        // --- Shared/Feature Properties ---
        private string _jobName = "";
        /// <summary>
        /// Gets or sets the display name of the backup job.
        /// </summary>
        public string JobName { get => _jobName; set => SetProperty(ref _jobName, value); }

        private double _progressPercentage = 0;
        /// <summary>
        /// Gets or sets the numerical progress percentage (0 to 100).
        /// Used primarily to update progress bars in the user interface.
        /// </summary>
        public double ProgressPercentage { get => _progressPercentage; set => SetProperty(ref _progressPercentage, value); }

        private string _status = "Waiting...";
        /// <summary>
        /// Gets or sets the current textual status message of the job.
        /// Defaults to "Waiting..." before execution starts.
        /// </summary>
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        private bool _isCompleted = false;
        /// <summary>
        /// Gets or sets a value indicating whether the backup job has successfully finished.
        /// </summary>
        public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }

        private bool _hasError = false;
        /// <summary>
        /// Gets or sets a value indicating whether an error occurred during the backup process.
        /// </summary>
        public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }
    }


    /// <summary>
    /// Main ViewModel for the application's primary window.
    /// Manages navigation, backup job configuration, application settings, and real-time execution monitoring.
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        // --- DYNAMIC TRANSLATION MANAGEMENT ---

        /// <summary>
        /// Gets the singleton instance of the localization manager (I18n).
        /// Used for dynamic translation in the XAML view.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        public I18n L => I18n.Instance;
#pragma warning restore CA1822 // Mark members as static

        /// <summary>
        /// Private reference to the core Backup Manager logic.
        /// </summary>
        private readonly BackupManager _backupManager;

        /// <summary>
        /// Gets or sets the collection of available backup jobs.
        /// This collection is bound to the job list in the user interface.
        /// </summary>
        public ObservableCollection<BackupJob> BackupJobs { get; set; }
        
        /// <summary>
        /// Gets the collection of progress items for active backup jobs.
        /// This collection is updated in real-time to reflect the status of each running job in the UI.
        /// </summary>
        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }


        private string _currentView = "Menu";
        /// <summary>
        /// Gets or sets the name of the currently active view (e.g., "Menu", "CreateJob", "Settings").
        /// Controls the view switching logic in the main window.
        /// </summary>
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _newJobName = "";
        /// <summary>
        /// Gets or sets the name of the new backup job being created or edited.
        /// </summary>
        public string NewJobName { get => _newJobName; set => SetProperty(ref _newJobName, value); }

        private string _newJobSource = "";
        /// <summary>
        /// Gets or sets the source directory path for the backup job.
        /// </summary>
        public string NewJobSource { get => _newJobSource; set => SetProperty(ref _newJobSource, value); }

        private string _newJobTarget = "";
        /// <summary>
        /// Gets or sets the target destination path for the backup job.
        /// </summary>
        public string NewJobTarget { get => _newJobTarget; set => SetProperty(ref _newJobTarget, value); }

        private int _selectedBackupType = 0;
        /// <summary>
        /// Gets or sets the type of backup selected (e.g., 0 for Full, 1 for Differential).
        /// </summary>
        public int SelectedBackupType { get => _selectedBackupType; set => SetProperty(ref _selectedBackupType, value); }

        private BackupJob? _selectedJob;
        /// <summary>
        /// Gets or sets the backup job currently selected in the user interface list.
        /// </summary>
        public BackupJob? SelectedJob { get => _selectedJob; set => SetProperty(ref _selectedJob, value); }

        // --- EXECUTION PROPERTIES ---
        
        private string _statusMessage = "";
        /// <summary>
        /// Gets or sets the current global status message displayed in the UI footer.
        /// </summary>
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isExecuting = false;
        /// <summary>
        /// Gets or sets a value indicating whether a backup operation is currently in progress.
        /// </summary>
        public bool IsExecuting { get => _isExecuting; set => SetProperty(ref _isExecuting, value); }

        private double _currentProgress = 0;
        /// <summary>
        /// Gets or sets the global progress percentage of the current execution.
        /// </summary>
        public double CurrentProgress { get => _currentProgress; set => SetProperty(ref _currentProgress, value); }

        private string _currentFileInfo = "";
        /// <summary>
        /// Gets or sets information about the file currently being processed.
        /// </summary>
        public string CurrentFileInfo { get => _currentFileInfo; set => SetProperty(ref _currentFileInfo, value); }

        // V3.0 Execution Extras
        
        private string _currentSizeInfo = "";
        /// <summary>
        /// Gets or sets information about the total size of the current backup selection.
        /// </summary>
        public string CurrentSizeInfo { get => _currentSizeInfo; set => SetProperty(ref _currentSizeInfo, value); }

        private bool _isSingleJobPaused = false;
        /// <summary>
        /// Gets or sets a value indicating whether the current single job execution is paused.
        /// </summary>
        public bool IsSingleJobPaused { get => _isSingleJobPaused; set => SetProperty(ref _isSingleJobPaused, value); }


        // --- SETTINGS PROPERTIES (Merged Feature + V3) ---
        
        private int _selectedLanguage = 0;
        /// <summary>
        /// Gets or sets the application language index (e.g., 0 for French, 1 for English).
        /// </summary>
        public int SelectedLanguage { get => _selectedLanguage; set => SetProperty(ref _selectedLanguage, value); }

        private int _selectedWindowMode = 1;
        /// <summary>
        /// Gets or sets the UI theme or window mode (Light/Dark mode).
        /// </summary>
        public int SelectedWindowMode { get => _selectedWindowMode; set => SetProperty(ref _selectedWindowMode, value); }

        private string _configBusinessSoftware = "";
        /// <summary>
        /// Gets or sets the process name of the business software that must trigger a backup pause if running.
        /// </summary>
        public string ConfigBusinessSoftware { get => _configBusinessSoftware; set => SetProperty(ref _configBusinessSoftware, value); }

        private string _configCryptoPath = "";
        public string ConfigCryptoPath { get => _configCryptoPath; set => SetProperty(ref _configCryptoPath, value); }

        private string _configCryptoKey = "";
        public string ConfigCryptoKey { get => _configCryptoKey; set => SetProperty(ref _configCryptoKey, value); }

        private long _configMaxSize = 1000;
        public long ConfigMaxSize { get => _configMaxSize; set => SetProperty(ref _configMaxSize, value); }

        private string _configPriorityExtensions = "";
        /// <summary>
        /// Gets or sets the list of file extensions that should be treated with priority during backup.
        /// </summary>
        public string ConfigPriorityExtensions { get => _configPriorityExtensions; set => SetProperty(ref _configPriorityExtensions, value); }

        private int _selectedLogFormat = 0;
        /// <summary>
        /// Gets or sets the format used for log files (0 for JSON, 1 for XML).
        /// </summary>
        public int SelectedLogFormat { get => _selectedLogFormat; set => SetProperty(ref _selectedLogFormat, value); }

        private int _selectedLogMode = 0;
        /// <summary>
        /// Gets or sets the log mode (0 for Local, 1 for Remote, 2 for Both).
        /// </summary>
        public int SelectedLogMode { get => _selectedLogMode; set => SetProperty(ref _selectedLogMode, value); }

        private string _configLogServerUrl = "http://localhost:5000";
        /// <summary>
        /// Gets or sets the URL of the centralized log server (Docker).
        /// </summary>
        public string ConfigLogServerUrl { get => _configLogServerUrl; set => SetProperty(ref _configLogServerUrl, value); }

        public ObservableCollection<string> LogFormats { get; } = ["JSON", "XML"];
        public ObservableCollection<string> LogModes { get; } = ["Local", "Remote", "Both"];

        /// <summary>Gets the list of available backup types for selection.</summary>
        public ObservableCollection<string> BackupTypes { get; private set; }
        
        /// <summary>Gets the list of available window display modes.</summary>
        public ObservableCollection<string> WindowModes { get; private set; }

        // --- DYNAMIC TRANSLATIONS (Properties from Feature Branch) ---
        
        /// <summary>
        /// Private instance of the localization service used to retrieve translated strings.
        /// </summary>
        private readonly I18n _i18n = I18n.Instance;

        public string AppTitle => _i18n.GetString("app_title");
        /// <summary>Localized string for the main menu title.</summary>
        public string MenuTitle => _i18n.GetString("menu_title");
        public string MenuDashboard => _i18n.GetString("menu_dashboard");
        public string MenuActionsTitle => _i18n.GetString("menu_actions_title");
        /// <summary>Localized string for the "Create" menu option.</summary>
        public string MenuCreate => _i18n.GetString("menu_create");
        /// <summary>Localized string for the "Execute" menu option.</summary>
        public string MenuExecute => _i18n.GetString("menu_execute");
        /// <summary>Localized string for the "Execute All" menu option.</summary>
        public string MenuExecuteAll => _i18n.GetString("menu_execute_all");
        /// <summary>Localized string for the "List" menu option.</summary>
        public string MenuList => _i18n.GetString("menu_list");
        /// <summary>Localized string for the "Delete" menu option.</summary>
        public string MenuDelete => _i18n.GetString("menu_delete");
        /// <summary>Localized string for the "Settings" menu option.</summary>
        public string MenuSettings => _i18n.GetString("menu_settings");
        /// <summary>Localized string for the "Exit" menu option.</summary>
        public string MenuExit => _i18n.GetString("menu_exit");
        
        /// <summary>Localized string for the Dark theme label.</summary>
        public string ThemeDark => _i18n.GetString("theme_dark");
        /// <summary>Localized string for the Light theme label.</summary>
        public string ThemeLight => _i18n.GetString("theme_light");
        
        public string DashboardWelcome => _i18n.GetString("dashboard_welcome");
        public string DashboardSubtitle => _i18n.GetString("dashboard_subtitle");
        
        /// <summary>Localized string for the job creation view title.</summary>
        public string CreateTitle => _i18n.GetString("create_title");
        /// <summary>Localized string for the job name label.</summary>
        public string CreateName => _i18n.GetString("create_name");
        /// <summary>Localized string for the source path label.</summary>
        public string CreateSource => _i18n.GetString("create_source");
        /// <summary>Localized string for the target path label.</summary>
        public string CreateTarget => _i18n.GetString("create_target");
        /// <summary>Localized string for the backup type label.</summary>
        public string CreateType => _i18n.GetString("create_type");
        /// <summary>Localized string for the job creation button.</summary>
        public string CreateButton => _i18n.GetString("create_button");
        public string BtnSave => _i18n.GetString("btn_save");
        /// <summary>Localized string for the general cancel button.</summary>
        public string BtnCancel => _i18n.GetString("btn_cancel");

        /// <summary>Localized string for the execution view title.</summary>
        public string ExecuteTitle => _i18n.GetString("execute_title");
        public string ExecuteSubtitle => _i18n.GetString("execute_subtitle");
        /// <summary>Localized string for the job selection label in execution view.</summary>
        public string ExecuteSelect => _i18n.GetString("execute_select");
        public string ExecutePlaceholder => _i18n.GetString("execute_placeholder");
        /// <summary>Localized string for the run button.</summary>
        public string ExecuteButton => _i18n.GetString("execute_button");
        /// <summary>Localized string displayed when no jobs are available for execution.</summary>
        public string ExecuteNoJobs => _i18n.GetString("execute_no_jobs");
        public string BtnStart => _i18n.GetString("btn_start");

        public string ExecuteAllTitle => _i18n.GetString("execute_all_title");
        public string ExecuteAllSubtitle => _i18n.GetString("execute_all_subtitle");
        public string BtnStartAll => _i18n.GetString("btn_start_all");

        /// <summary>Localized string for the job list view title.</summary>
        public string ListTitle => _i18n.GetString("list_title");
        /// <summary>Localized string for the ID column header.</summary>
        public string ListId => _i18n.GetString("list_id");
        /// <summary>Localized string for the name column header.</summary>
        public string ListName => _i18n.GetString("list_name");
        /// <summary>Localized string for the source path column header.</summary>
        public string ListSource => _i18n.GetString("list_source");
        /// <summary>Localized string for the target path column header.</summary>
        public string ListTarget => _i18n.GetString("list_target");

        /// <summary>Localized string for the deletion view title.</summary>
        public string DeleteTitle => _i18n.GetString("delete_title");
        /// <summary>Localized string for the job selection label in deletion view.</summary>
        public string DeleteSelect => _i18n.GetString("delete_select");
        /// <summary>Localized string for the delete confirmation button.</summary>
        public string DeleteButton => _i18n.GetString("delete_button");
        /// <summary>Localized string for the deletion warning message.</summary>
        public string DeleteWarning => _i18n.GetString("delete_warning");
        /// <summary>Localized string displayed when no jobs are available for deletion.</summary>
        public string DeleteNoJobs => _i18n.GetString("delete_no_jobs");
        public string BtnDelete => _i18n.GetString("btn_delete");

        // Settings Strings
        /// <summary>Localized string for the application settings title.</summary>
        public string SettingsTitle => _i18n.GetString("settings_title");
        public string SettingsLang => _i18n.GetString("settings_lang");
        public string SettingsWindowMode => _i18n.GetString("settings_window_mode");
        public string SettingsGeneral => _i18n.GetString("settings_general");
        public string SettingsBusinessSoft => _i18n.GetString("settings_business_soft");
        public string SettingsLogFormat => _i18n.GetString("settings_log_format");
        public string SettingsLogs => _i18n.GetString("settings_logs");
        public string SettingsLogMode => _i18n.GetString("settings_log_mode");
        public string SettingsLogServerUrl => _i18n.GetString("settings_log_server_url");
        public string SettingsCrypto => _i18n.GetString("settings_crypto");
        public string SettingsCryptoPath => _i18n.GetString("settings_crypto_path");
        public string SettingsCryptoKey => _i18n.GetString("settings_crypto_key");
        public string SettingsPerformance => _i18n.GetString("settings_performance");
        public string SettingsMaxSize => _i18n.GetString("settings_max_size");
        public string SettingsPriorityExt => _i18n.GetString("settings_priority_ext");
        public string BtnApply => _i18n.GetString("btn_apply");

        /// <summary>Localized string for the language settings title.</summary>
        public string LanguageTitle => _i18n.GetString("language_title");
        /// <summary>Localized string for the language selection label.</summary>
        public string LanguageSelect => _i18n.GetString("language_select");
        /// <summary>Localized string for the apply language button.</summary>
        public string LanguageApply => _i18n.GetString("language_apply");
        /// <summary>Localized string for the general back button.</summary>
        public string ButtonBack => _i18n.GetString("button_back");

        /// <summary>Localized string for the "Full" backup type.</summary>
        public string TypeComplete => _i18n.GetString("type_complete");
        /// <summary>Localized string for the "Differential" backup type.</summary>
        public string TypeDifferential => _i18n.GetString("type_differential");


        // --- COMMANDS ---

        /// <summary>Command to switch the application's visual theme.</summary>
        public ICommand SwitchThemeCommand { get; }
        /// <summary>Command to navigate to the job creation view.</summary>
        public ICommand CreateBackupJobCommand { get; }
        /// <summary>Command to navigate to the job execution view.</summary>
        public ICommand ExecuteBackupJobCommand { get; }
        /// <summary>Command to initiate the execution of all configured backup jobs.</summary>
        public ICommand ExecuteAllBackupJobsCommand { get; }
        /// <summary>Command to start the execution process for all jobs (V3.0 Dashboard).</summary>
        public ICommand StartAllBackupJobsCommand { get; }
        /// <summary>Command to navigate to the backup job list view.</summary>
        public ICommand ListAllBackupJobsCommand { get; }
        /// <summary>Command to navigate to the backup job deletion view.</summary>
        public ICommand DeleteBackupJobCommand { get; }
        /// <summary>Command to navigate to the application settings view.</summary>
        public ICommand OpenSettingsCommand { get; }
        /// <summary>Command to close the application.</summary>
        public ICommand ExitCommand { get; }
        /// <summary>Command to navigate back to the main menu.</summary>
        public ICommand BackToMenuCommand { get; }
        /// <summary>Command to save a new or edited backup job to the configuration.</summary>
        public ICommand SaveNewJobCommand { get; }
        /// <summary>Command to execute the currently selected backup job.</summary>
        public ICommand ExecuteSelectedJobCommand { get; }
        /// <summary>Command to delete the currently selected backup job.</summary>
        public ICommand DeleteSelectedJobCommand { get; }
        /// <summary>Command to apply the general application settings.</summary>
        public ICommand ApplySettingsCommand { get; }

        // V3.0 Control Commands
        /// <summary>Command to pause a specific backup job by its ID.</summary>
        public ICommand PauseJobCommand { get; }
        /// <summary>Command to resume a specific backup job by its ID.</summary>
        public ICommand ResumeJobCommand { get; }
        /// <summary>Command to stop a specific backup job by its ID.</summary>
        public ICommand StopJobCommand { get; }
        /// <summary>Command to pause all currently running backup jobs.</summary>
        public ICommand PauseAllCommand { get; }
        /// <summary>Command to resume all currently paused backup jobs.</summary>
        public ICommand ResumeAllCommand { get; }
        /// <summary>Command to stop all currently running backup jobs.</summary>
        public ICommand StopAllCommand { get; }
        /// <summary>Command to toggle between pause and resume for a specific job.</summary>
        public ICommand TogglePauseJobCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// Sets up the backup manager, initializes collections, and binds commands to their respective logic.
        /// </summary>
        public MainWindowViewModel()
        {
            _backupManager = BackupManager.GetBM();

            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new(jobsFromManager);
            JobsProgress = [];

            // Initialize Collections
            BackupTypes = [TypeComplete, TypeDifferential];
            WindowModes = [
                _i18n.GetString("settings_window_windowed"),
                _i18n.GetString("settings_window_maximized"),
                _i18n.GetString("settings_window_fullscreen")
            ];

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

        /// <summary>
        /// Switches the application's visual theme (Light, Dark, or Default).
        /// Updates the RequestedThemeVariant of the current Avalonia application instance.
        /// </summary>
        /// <param name="theme">The name of the theme to apply ("Light" or "Dark").</param>
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

        /// <summary>
        /// Synchronizes the local BackupJobs collection with the data from the BackupManager.
        /// Clears the current list and reloads all jobs to ensure the UI is up to date.
        /// </summary>
        private void RefreshBackupJobs()
        {
            BackupJobs.Clear();
            foreach (var job in _backupManager.GetAllJobs())
            {
                BackupJobs.Add(job);
            }
        }

        private static async Task ApplyWindowModeLogic(int mode)
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

        /// <summary>
        /// Validates and saves a new backup job using the provided input fields.
        /// Checks for duplicate names and updates the UI based on the success or failure of the operation.
        /// </summary>
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

        /// <summary>
        /// Executes the currently selected backup job asynchronously.
        /// Manages real-time progress updates, byte calculations, and UI state transitions 
        /// using the Avalonia Dispatcher to ensure thread safety.
        /// </summary>
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

        /// <summary>
        /// Prepares the UI for the execution of all backup jobs.
        /// Clears and populates the JobsProgress collection with waiting status items.
        /// </summary>
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

        /// <summary>
        /// Starts the asynchronous execution of all backup jobs.
        /// Updates individual JobProgressItem objects in real-time and calculates byte-based progress.
        /// </summary>
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

        /// <summary>
        /// Asynchronously deletes the currently selected backup job from the manager.
        /// Resets the selection and updates the UI upon success.
        /// </summary>
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

        /// <summary>
        /// Loads all application settings from the ConfigManager into the ViewModel properties.
        /// Includes window mode, log format, business software, and priority extensions.
        /// </summary>
        private void OpenSettings()
        {
            var config = _backupManager.ConfigManager;
            
            // Load detailed settings (Feature Branch)
            ConfigBusinessSoftware = config.GetConfig<string>("BusinessSoftware") ?? "";
            ConfigCryptoPath = config.GetConfig<string>("CryptoSoftPath") ?? "";
            ConfigCryptoKey = config.GetConfig<string>("CryptoKey") ?? "";
            ConfigMaxSize = config.GetConfig<long?>("MaxParallelTransferSize") ?? 1000;

            var extensions = config.GetConfig<List<string>>("PriorityExtensions") ?? [];
            ConfigPriorityExtensions = string.Join(", ", extensions);

            string logFormat = config.GetConfig<string>("LoggerFormat") ?? "json";
            SelectedLogFormat = logFormat.Equals("xml", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            string logMode = config.GetConfig<string>("LogMode") ?? "Local";
            if (logMode.Equals("remote", StringComparison.OrdinalIgnoreCase))
                SelectedLogMode = 1;
            else if (logMode.Equals("both", StringComparison.OrdinalIgnoreCase))
                SelectedLogMode = 2;
            else
                SelectedLogMode = 0;

            ConfigLogServerUrl = config.GetConfig<string>("LogServerUrl") ?? "http://localhost:5000";

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

        /// <summary>
        /// Saves all modified settings back to the configuration file and applies them immediately.
        /// Manages window state transitions and list serialization for extensions.
        /// </summary>
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

                string logModeValue = SelectedLogMode switch
                {
                    1 => "Remote",
                    2 => "Both",
                    _ => "Local"
                };
                config.SetConfig("LogMode", logModeValue);
                config.SetConfig("LogServerUrl", ConfigLogServerUrl);

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

        /// <summary>
        /// Triggers property change notifications for all localized strings and collections.
        /// Essential for updating the UI after a language switch.
        /// </summary>
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
            OnPropertyChanged(nameof(SettingsLogs));
            OnPropertyChanged(nameof(SettingsLogMode));
            OnPropertyChanged(nameof(SettingsLogServerUrl));
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
            foreach (var item in new[] { _i18n.GetString("type_complete"), _i18n.GetString("type_differential") })
            {
                BackupTypes.Add(item);
            }

            WindowModes.Clear();
            foreach (var item in new[]
            {
                _i18n.GetString("settings_window_windowed"),
                _i18n.GetString("settings_window_maximized"),
                _i18n.GetString("settings_window_fullscreen")
            })
            {
                WindowModes.Add(item);
            }
        }

        /// <summary>
        /// Shuts down the application via the Desktop Application Lifetime.
        /// </summary>
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) 
                desktop.Shutdown();
        }

        // --- HELPER METHODS FROM V3.0 ---

        /// <summary>
        /// Toggles the pause/resume state for a specific backup job by ID.
        /// Immediately updates the local UI state before the background task confirms the change.
        /// </summary>
        /// <param name="id">The unique identifier of the backup job.</param>
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

        /// <summary>
        /// Helper method to force-update UI components (icons/messages) for better user responsiveness.
        /// Updates both the single-job view and the mass-execution dashboard.
        /// </summary>
        /// <param name="jobId">ID of the job to update.</param>
        /// <param name="isPaused">The new pause status.</param>
        /// <param name="message">The status message to display.</param>
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

        /// <summary>
        /// Converts a long value in bytes into a human-readable string (B, KB, MB, GB, TB).
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        /// <returns>A formatted string with two decimal places and the appropriate suffix.</returns>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = [ "B", "KB", "MB", "GB", "TB" ];
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n2} {1}", number, suffixes[counter]);
        }
    }
}