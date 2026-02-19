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


    /// <summary>
    /// Represents the progress status of a specific backup job.
    /// Used to track and display real-time updates in the UI for individual jobs.
    /// This class inherits from ObservableObject to support data binding in the GUI.
    /// </summary>
    public class JobProgressItem : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique identifier of the backup job.
        /// Required for individual control commands such as Pause, Resume, or Stop.
        /// </summary>
        public int JobId { get; set; }

        private string _jobName = "";
        /// <summary>
        /// Gets or sets the display name of the backup job.
        /// </summary>
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        private string _progressBytes = "";
        /// <summary>
        /// Gets or sets the formatted string representing the progress in bytes (e.g., "50MB / 100MB").
        /// </summary>
        public string ProgressBytes
        {
            get => _progressBytes;
            set => SetProperty(ref _progressBytes, value);
        }

        private double _progressPercentage = 0;
        /// <summary>
        /// Gets or sets the numerical progress percentage (0 to 100).
        /// Used primarily to update progress bars in the user interface.
        /// </summary>
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _status = "Waiting...";
        /// <summary>
        /// Gets or sets the current textual status message of the job.
        /// Defaults to "Waiting..." before execution starts.
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isCompleted = false;
        /// <summary>
        /// Gets or sets a value indicating whether the backup job has successfully finished.
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        private bool _hasError = false;
        /// <summary>
        /// Gets or sets a value indicating whether an error occurred during the backup process.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private bool _isPaused = false;
        /// <summary>
        /// Gets or sets a value indicating whether the backup job is currently paused.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }
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
        public I18n L => I18n.Instance;

        /// <summary>
        /// Gets the default welcome message displayed in the view.
        /// </summary>
        public string Greeting { get; } = "Welcome to EasySave!";

        /// <summary>
        /// Private reference to the core Backup Manager logic.
        /// </summary>
        private readonly BackupManager _backupManager;

        /// <summary>
        /// Gets or sets the collection of available backup jobs.
        /// This collection is bound to the job list in the user interface.
        /// </summary>
        public ObservableCollection<BackupJob> BackupJobs { get; set; }

        // Navigation

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
        public string NewJobName
        {
            get => _newJobName;
            set => SetProperty(ref _newJobName, value);
        }

        private string _currentSizeInfo = "";
        /// <summary>
        /// Gets or sets information about the total size of the current backup selection.
        /// </summary>
        public string CurrentSizeInfo
        {
            get => _currentSizeInfo;
            set => SetProperty(ref _currentSizeInfo, value);
        }

        private string _newJobSource = "";
        /// <summary>
        /// Gets or sets the source directory path for the backup job.
        /// </summary>
        public string NewJobSource
        {
            get => _newJobSource;
            set => SetProperty(ref _newJobSource, value);
        }

        private string _newJobTarget = "";
        /// <summary>
        /// Gets or sets the target destination path for the backup job.
        /// </summary>
        public string NewJobTarget
        {
            get => _newJobTarget;
            set => SetProperty(ref _newJobTarget, value);
        }

        private int _selectedBackupType = 0;
        /// <summary>
        /// Gets or sets the type of backup selected (e.g., 0 for Full, 1 for Differential).
        /// </summary>
        public int SelectedBackupType
        {
            get => _selectedBackupType;
            set => SetProperty(ref _selectedBackupType, value);
        }

        private BackupJob? _selectedJob;
        /// <summary>
        /// Gets or sets the backup job currently selected in the user interface list.
        /// </summary>
        public BackupJob? SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        // --- SETTINGS PROPERTIES (COMPLET V1.1, V2.0, V3.0) ---

        private int _selectedLanguage = 0;
        /// <summary>
        /// Gets or sets the application language index (e.g., 0 for French, 1 for English).
        /// </summary>
        public int SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        private int _selectedWindowMode = 1;
        /// <summary>
        /// Gets or sets the UI theme or window mode (Light/Dark mode).
        /// </summary>
        public int SelectedWindowMode
        {
            get => _selectedWindowMode;
            set => SetProperty(ref _selectedWindowMode, value);
        }

        // V1.1 : Format Log (JSON/XML)
        private int _selectedLogFormat = 0; // 0=JSON, 1=XML
        /// <summary>
        /// Gets or sets the format used for log files (0 for JSON, 1 for XML).
        /// Introduced in V1.1.
        /// </summary>
        public int SelectedLogFormat
        {
            get => _selectedLogFormat;
            set => SetProperty(ref _selectedLogFormat, value);
        }

        // V2.0/V3.0 : Logiciel Métier
        private string _businessSoftware = "";
        /// <summary>
        /// Gets or sets the process name of the business software that must trigger a backup pause if running.
        /// Introduced in V2.0.
        /// </summary>
        public string BusinessSoftware
        {
            get => _businessSoftware;
            set => SetProperty(ref _businessSoftware, value);
        }

        // V2.0/V3.0 : Extensions Prioritaires / Cryptées
        private string _priorityExtensions = "";
        /// <summary>
        /// Gets or sets the list of file extensions that should be treated with priority during backup.
        /// Introduced in V2.0.
        /// </summary>
        public string PriorityExtensions
        {
            get => _priorityExtensions;
            set => SetProperty(ref _priorityExtensions, value);
        }

        private string _statusMessage = "";
        /// <summary>
        /// Gets or sets the current global status message displayed in the UI footer.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isExecuting = false;
        /// <summary>
        /// Gets or sets a value indicating whether a backup operation is currently in progress.
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            set => SetProperty(ref _isExecuting, value);
        }

        private double _currentProgress = 0;
        /// <summary>
        /// Gets or sets the global progress percentage of the current execution.
        /// </summary>
        public double CurrentProgress
        {
            get => _currentProgress;
            set => SetProperty(ref _currentProgress, value);
        }

        private string _currentFileInfo = "";
        /// <summary>
        /// Gets or sets information about the file currently being processed.
        /// </summary>
        public string CurrentFileInfo
        {
            get => _currentFileInfo;
            set => SetProperty(ref _currentFileInfo, value);
        }

        private bool _isSingleJobPaused = false;
        /// <summary>
        /// Gets or sets a value indicating whether the current single job execution is paused.
        /// </summary>
        public bool IsSingleJobPaused
        {
            get => _isSingleJobPaused;
            set => SetProperty(ref _isSingleJobPaused, value);
        }

        /// <summary>
        /// Gets the collection of progress items for active backup jobs.
        /// This collection is updated in real-time to reflect the status of each running job in the UI.
        /// </summary>
        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }

        /// <summary>
        /// Private instance of the localization service used to retrieve translated strings.
        /// </summary>
        private I18n _i18n = I18n.Instance;

        // Localized Strings

        /// <summary>Localized string for the main menu title.</summary>
        public string MenuTitle => _i18n.GetString("menu_title");
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
        /// <summary>Localized string for the "Language" menu option.</summary>
        public string MenuLanguage => _i18n.GetString("menu_language");
        /// <summary>Localized string for the "Exit" menu option.</summary>
        public string MenuExit => _i18n.GetString("menu_exit");
        /// <summary>Localized string for the Dark theme label.</summary>
        public string ThemeDark => _i18n.GetString("theme_dark");
        /// <summary>Localized string for the Light theme label.</summary>
        public string ThemeLight => _i18n.GetString("theme_light");
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
        /// <summary>Localized string for the execution view title.</summary>
        public string ExecuteTitle => _i18n.GetString("execute_title");
        /// <summary>Localized string for the job selection label in execution view.</summary>
        public string ExecuteSelect => _i18n.GetString("execute_select");
        /// <summary>Localized string for the run button.</summary>
        public string ExecuteButton => _i18n.GetString("execute_button");
        /// <summary>Localized string displayed when no jobs are available for execution.</summary>
        public string ExecuteNoJobs => _i18n.GetString("execute_no_jobs");
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
        /// <summary>Localized string for the language settings title.</summary>
        public string LanguageTitle => _i18n.GetString("language_title");
        /// <summary>Localized string for the language selection label.</summary>
        public string LanguageSelect => _i18n.GetString("language_select");
        /// <summary>Localized string for the apply language button.</summary>
        public string LanguageApply => _i18n.GetString("language_apply");
        /// <summary>Localized string for the general cancel button.</summary>
        public string ButtonCancel => _i18n.GetString("button_cancel");
        /// <summary>Localized string for the general back button.</summary>
        public string ButtonBack => _i18n.GetString("button_back");
        /// <summary>Localized string for the "Full" backup type.</summary>
        public string TypeComplete => _i18n.GetString("type_complete");
        /// <summary>Localized string for the "Differential" backup type.</summary>
        public string TypeDifferential => _i18n.GetString("type_differential");

        /// <summary>Command to toggle between pause and resume for a specific job.</summary>
        public ICommand TogglePauseJobCommand { get; }

        /// <summary>Gets the list of available backup types for selection.</summary>
        public ObservableCollection<string> BackupTypes { get; private set; }
        /// <summary>Gets the list of available window display modes.</summary>
        public ObservableCollection<string> WindowModes { get; private set; }

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
        /// <summary>Command to navigate to the language selection view.</summary>
        public ICommand ChangeLanguageCommand { get; }
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
        /// <summary>Command to apply the selected language settings.</summary>
        public ICommand ApplyLanguageCommand { get; }
        /// <summary>Command to apply the general application settings.</summary>
        public ICommand ApplySettingsCommand { get; }

        // V3.0 : Commandes de contrôle Temps Réel

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// Sets up the backup manager, initializes collections, and binds commands to their respective logic.
        /// </summary>
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
            PauseJobCommand = new RelayCommand<int>(id => _backupManager.PauseJob(id));
            ResumeJobCommand = new RelayCommand<int>(id => _backupManager.ResumeJob(id));
            StopJobCommand = new RelayCommand<int>(id => _backupManager.StopJob(id));

            PauseAllCommand = new RelayCommand(() => _backupManager.PauseAllJobs());
            ResumeAllCommand = new RelayCommand(() => _backupManager.ResumeAllJobs());
            StopAllCommand = new RelayCommand(() => _backupManager.StopAllJobs());
            TogglePauseJobCommand = new RelayCommand<int>(TogglePauseJob);
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

        /// <summary>
        /// Navigates back to the main menu view and resets the global status message.
        /// </summary>
        private void BackToMenu()
        {
            CurrentView = "Menu";
            StatusMessage = "";
        }

        /// <summary>
        /// Prepares the interface for creating a new backup job.
        /// Switches the view and resets all input fields to their default values.
        /// </summary>
        private void CreateBackupJob()
        {
            CurrentView = "CreateJob";
            NewJobName = "";
            NewJobSource = "";
            NewJobTarget = "";
            SelectedBackupType = 0;
            StatusMessage = "";
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
        /// Navigates to the job execution view.
        /// Refreshes the job list to ensure the user can select from the latest configuration.
        /// </summary>
        private void ExecuteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ExecuteJob";
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
                                long processedBytes = progress.TotalSize - progress.SizeRemaining;

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
                    JobId = job.Id, // V3.0: Important for individual command binding
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
                                // --- MODIFICATION : BYTE-BASED PERCENTAGE CALCULATION ---
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

        /// <summary>
        /// Navigates to the backup job list view after refreshing data from the manager.
        /// </summary>
        private void ListAllBackupJobs()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ListJobs";
        }

        /// <summary>
        /// Navigates to the backup job deletion view.
        /// </summary>
        private void DeleteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "DeleteJob";
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
        /// Initializes the language selection view based on current settings.
        /// </summary>
        private void ChangeLanguage()
        {
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;
            CurrentView = "ChangeLanguage";
            StatusMessage = "";
        }

        /// <summary>
        /// Loads all application settings from the ConfigManager into the ViewModel properties.
        /// Includes window mode, log format, business software, and priority extensions.
        /// </summary>
        private void OpenSettings()
        {
            // Language and WindowMode
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

            // Loading Version-specific settings (V1.1 / V2.0 / V3.0)
            string currentFormat = _backupManager.ConfigManager.GetConfig<string>("LoggerFormat");
            SelectedLogFormat = currentFormat == "xml" ? 1 : 0;

            BusinessSoftware = _backupManager.ConfigManager.GetConfig<string>("BusinessSoftware") ?? "";

            var extensions = _backupManager.ConfigManager.GetConfig<List<string>>("PriorityExtensions");
            PriorityExtensions = extensions != null ? string.Join(", ", extensions) : "";

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
                int savedWindowMode = SelectedWindowMode;
                var i18n = I18n.Instance;

                // 1. Language
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

                // 3. Log Format (V1.1)
                string newFormat = SelectedLogFormat == 1 ? "xml" : "json";
                _backupManager.ConfigManager.SetConfig("LoggerFormat", newFormat);

                // 4. Business Software (V2.0/V3.0)
                _backupManager.ConfigManager.SetConfig("BusinessSoftware", BusinessSoftware);

                // 5. Extensions (V2.0/V3.0)
                var extList = PriorityExtensions.Split(',')
                                                .Select(e => e.Trim())
                                                .Where(e => !string.IsNullOrEmpty(e))
                                                .ToList();
                _backupManager.ConfigManager.SetConfig("PriorityExtensions", extList);

                // Disk persistence
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

        /// <summary>
        /// Applies the selected language and refreshes all UI translations.
        /// </summary>
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

        /// <summary>
        /// Triggers property change notifications for all localized strings and collections.
        /// Essential for updating the UI after a language switch.
        /// </summary>
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

        /// <summary>
        /// Shuts down the application via the Desktop Application Lifetime.
        /// </summary>
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        // --- NEW METHODS FOR PLAY/PAUSE BUTTON ---

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
                    UpdateUiState(id, false, _i18n.GetString("status_running"));
                }
                else
                {
                    _backupManager.PauseJob(id);
                    UpdateUiState(id, true, _i18n.GetString("status_paused_user"));
                }
            }
        }

        private bool _allPaused = false;
        /// <summary>
        /// Toggles the pause/resume state for all currently running backup jobs.
        /// </summary>
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