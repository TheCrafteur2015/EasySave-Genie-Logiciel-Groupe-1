using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using EasySave.View.Localization; // Necessary for I18n
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private string _jobName = "";

        /// <summary>
        /// Gets or sets the name of the backup job.
        /// </summary>
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        private double _progressPercentage = 0;

        /// <summary>
        /// Gets or sets the current completion percentage of the job (0-100).
        /// </summary>
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _status = "Waiting...";

        /// <summary>
        /// Gets or sets the textual status of the job (e.g., "Waiting...", "Active", "Completed").
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isCompleted = false;

        /// <summary>
        /// Gets or sets a value indicating whether the job has completed successfully.
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        private bool _hasError = false;

        /// <summary>
        /// Gets or sets a value indicating whether the job encountered an error during execution.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }
    }

    /// <summary>
    /// Main ViewModel for the application's main window.
    /// Implements the MVVM pattern to handle UI logic, state management, commands, and navigation.
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        // --- DYNAMIC TRANSLATION MANAGEMENT ---

        /// <summary>
        /// Gets the internationalization instance.
        /// Allows binding to translated strings in XAML using the indexer syntax {Binding L[key]}.
        /// </summary>
        public I18n L => I18n.Instance;

        /// <summary>
        /// Gets the default greeting message.
        /// </summary>
        public string Greeting { get; } = "Welcome to EasySave!";

        private readonly BackupManager _backupManager;

        /// <summary>
        /// Gets or sets the collection of backup jobs displayed in the UI.
        /// </summary>
        public ObservableCollection<BackupJob> BackupJobs { get; set; }

        // Navigation
        private string _currentView = "Menu";

        /// <summary>
        /// Gets or sets the current view identifier to control UI navigation (e.g., "Menu", "CreateJob").
        /// </summary>
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        // --- FORM PROPERTIES ---
        private string _newJobName = "";

        /// <summary>
        /// Gets or sets the name for a new backup job being created.
        /// </summary>
        public string NewJobName
        {
            get => _newJobName;
            set => SetProperty(ref _newJobName, value);
        }

        private string _newJobSource = "";

        /// <summary>
        /// Gets or sets the source directory path for a new backup job.
        /// </summary>
        public string NewJobSource
        {
            get => _newJobSource;
            set => SetProperty(ref _newJobSource, value);
        }

        private string _newJobTarget = "";

        /// <summary>
        /// Gets or sets the target directory path for a new backup job.
        /// </summary>
        public string NewJobTarget
        {
            get => _newJobTarget;
            set => SetProperty(ref _newJobTarget, value);
        }

        private int _selectedBackupType = 0;

        /// <summary>
        /// Gets or sets the selected index for the backup type.
        /// 0 for Complete, 1 for Differential.
        /// </summary>
        public int SelectedBackupType
        {
            get => _selectedBackupType;
            set => SetProperty(ref _selectedBackupType, value);
        }

        private BackupJob? _selectedJob;

        /// <summary>
        /// Gets or sets the currently selected backup job from the list.
        /// </summary>
        public BackupJob? SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        private int _selectedLanguage = 0;

        /// <summary>
        /// Gets or sets the selected language index.
        /// 0 for English, 1 for French.
        /// </summary>
        public int SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        private int _selectedWindowMode = 1; // 0=Windowed, 1=Maximized (default), 2=Fullscreen

        /// <summary>
        /// Gets or sets the selected window mode index.
        /// 0: Windowed, 1: Maximized (default), 2: Fullscreen.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the status message displayed to the user (success, error, or information).
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isExecuting = false;

        /// <summary>
        /// Gets or sets a value indicating whether a backup operation is currently executing.
        /// Used to disable specific UI controls during execution.
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            set => SetProperty(ref _isExecuting, value);
        }

        private double _currentProgress = 0;

        /// <summary>
        /// Gets or sets the current progress percentage of the executing job (for single job execution).
        /// </summary>
        public double CurrentProgress
        {
            get => _currentProgress;
            set => SetProperty(ref _currentProgress, value);
        }

        private string _currentFileInfo = "";

        /// <summary>
        /// Gets or sets the information string about the file currently being processed.
        /// </summary>
        public string CurrentFileInfo
        {
            get => _currentFileInfo;
            set => SetProperty(ref _currentFileInfo, value);
        }

        /// <summary>
        /// Gets the collection tracking the progress of all active backup jobs (for parallel execution).
        /// </summary>
        public ObservableCollection<JobProgressItem> JobsProgress { get; private set; }

        // Localized string properties for UI binding
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

        /// <summary>
        /// Gets the collection of available backup types for display in ComboBoxes.
        /// </summary>
        public ObservableCollection<string> BackupTypes { get; private set; }

        /// <summary>
        /// Gets the collection of available window modes for display in ComboBoxes.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// Sets up the BackupManager, data collections, commands, and default selections.
        /// </summary>
        public MainWindowViewModel()
        {
            // 1. Retrieve Singleton BackupManager
            _backupManager = BackupManager.GetBM();

            // 2. Initialize observable list of jobs
            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);

            // 2b. Initialize progress collection
            JobsProgress = new ObservableCollection<JobProgressItem>();

            // 3. Initialize backup types collection
            BackupTypes = new ObservableCollection<string>
            {
                TypeComplete,
                TypeDifferential
            };

            // 4. Initialize window modes collection
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

        // --- METHODS ---

        /// <summary>
        /// Switches the application theme between Light and Dark modes.
        /// </summary>
        /// <param name="theme">The theme to switch to ("Light" or "Dark").</param>
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
        /// Refreshes the list of backup jobs from the BackupManager.
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
        /// Navigates back to the main menu view.
        /// </summary>
        private void BackToMenu()
        {
            CurrentView = "Menu";
            StatusMessage = "";
        }

        /// <summary>
        /// Navigates to the job creation view and resets form fields.
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
        /// Attempts to create a new backup job with the entered details.
        /// Validates input and adds the job via BackupManager.
        /// </summary>
        private async void SaveNewJob()
        {
            BackupType type = SelectedBackupType == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = _backupManager.AddJob(NewJobName, NewJobSource, NewJobTarget, type);

            if (success)
            {
                RefreshBackupJobs();
                StatusMessage = "✓ " + (_i18n.GetString("create_success") ?? "Job created successfully!");

                // Wait 2 seconds to display message
                await Task.Delay(2000);

                // Reset form to allow creating another job
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

        /// <summary>
        /// Navigates to the single job execution view.
        /// </summary>
        private void ExecuteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ExecuteJob";
        }

        /// <summary>
        /// Starts the execution of the currently selected backup job asynchronously.
        /// Updates progress and status on the UI thread.
        /// </summary>
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
                        // Execution in a separate thread to avoid blocking UI
                        _backupManager.ExecuteJob(jobId, progress =>
                        {
                            // Update progress on UI thread
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                CurrentProgress = progress.ProgressPercentage;
                                int filesProcessed = progress.TotalFiles - progress.FilesRemaining;
                                CurrentFileInfo = $"{filesProcessed}/{progress.TotalFiles} files";
                                StatusMessage = $"Running: {progress.ProgressPercentage:F1}% - {filesProcessed}/{progress.TotalFiles}";
                            });
                        });

                        // Final update on UI thread
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

        /// <summary>
        /// Prepares the UI for executing all backup jobs and navigates to the execution view.
        /// Initializes the progress list for all jobs.
        /// </summary>
        private void ExecuteAllBackupJobs()
        {
            // Prepare progress list
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

        /// <summary>
        /// Launches the execution of all backup jobs sequentially in a background task.
        /// Updates the status of each job in the JobsProgress collection.
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
                        // Find corresponding job in JobsProgress
                        var progressItem = JobsProgress.FirstOrDefault(j => j.JobName == progress.BackupName);
                        if (progressItem != null)
                        {
                            progressItem.ProgressPercentage = progress.ProgressPercentage;
                            int filesCopied = progress.TotalFiles - progress.FilesRemaining;
                            progressItem.Status = $"{progress.ProgressPercentage:F1}% - {filesCopied}/{progress.TotalFiles} files";

                            // If completed 100%
                            if (progress.ProgressPercentage >= 100)
                            {
                                progressItem.IsCompleted = true;
                                progressItem.Status = _i18n.GetString("execute_completed") ?? "Completed!";
                            }
                        }
                    });

                    // Mark all as completed
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

                    // Mark unfinished jobs as error
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

        /// <summary>
        /// Navigates to the job list view.
        /// </summary>
        private void ListAllBackupJobs()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "ListJobs";
        }

        /// <summary>
        /// Navigates to the job deletion view.
        /// </summary>
        private void DeleteBackupJob()
        {
            RefreshBackupJobs();
            StatusMessage = "";
            CurrentView = "DeleteJob";
        }

        /// <summary>
        /// Deletes the currently selected backup job via the BackupManager.
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

                    // Wait 2 seconds to display message
                    await Task.Delay(2000);

                    // Reset selection and message
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
        /// Navigates to the language selection view.
        /// </summary>
        private void ChangeLanguage()
        {
            // Load current language into selector
            // If language is "fr_fr", index is 1, else 0
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;

            CurrentView = "ChangeLanguage";
            StatusMessage = "";
        }

        /// <summary>
        /// Navigates to the settings view and initializes current settings values.
        /// </summary>
        private void OpenSettings()
        {
            // Load current settings
            SelectedLanguage = I18n.Instance.Language == "fr_fr" ? 1 : 0;

            // Detect current window mode
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    // If ExtendClientArea is active, it's borderless fullscreen mode
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

        /// <summary>
        /// Applies the selected settings (language and window mode) to the application.
        /// </summary>
        private async void ApplySettings()
        {
            try
            {
                // IMPORTANT: Save SelectedWindowMode BEFORE changing language
                // because RefreshTranslations() will reset the ComboBox
                int savedWindowMode = SelectedWindowMode;

                // Apply language
                var i18n = I18n.Instance;
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);
                RefreshTranslations();

                // Restore SelectedWindowMode after refresh
                SelectedWindowMode = savedWindowMode;

                // Apply window mode
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

                            case 1: // Maximized (with borders)
                                mainWindow.ExtendClientAreaToDecorationsHint = false;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;

                            case 2: // Fullscreen borderless
                                mainWindow.ExtendClientAreaToDecorationsHint = true;
                                mainWindow.ExtendClientAreaTitleBarHeightHint = -1;
                                mainWindow.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
                                await Task.Delay(50);
                                mainWindow.WindowState = WindowState.Maximized;
                                break;
                        }
                    }
                }

                // Display confirmation message
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
        /// Applies the selected language and updates the UI.
        /// </summary>
        private void ApplyLanguage()
        {
            try
            {
                // Get I18n instance
                var i18n = I18n.Instance;

                // Apply selected language
                string languageCode = SelectedLanguage == 0 ? "en_us" : "fr_fr";
                i18n.SetLanguage(languageCode);

                // Notify all translation properties to refresh interface
                RefreshTranslations();

                // Display message in newly selected language
                StatusMessage = i18n.GetString("language_changed");

                // Wait a bit for user to see the message
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
        /// Updates local properties dependent on translation when the language changes.
        /// </summary>
        private void RefreshTranslations()
        {
            // I18n object now notifies its own changes automatically (Item[])
            // Notify L just in case some bindings need it
            OnPropertyChanged(nameof(L));

            // Update BackupTypes collection with new translations
            BackupTypes.Clear();
            BackupTypes.Add(TypeComplete);
            BackupTypes.Add(TypeDifferential);

            // Update WindowModes collection with new translations
            WindowModes.Clear();
            WindowModes.Add(_i18n.GetString("settings_window_windowed"));
            WindowModes.Add(_i18n.GetString("settings_window_maximized"));
            WindowModes.Add(_i18n.GetString("settings_window_fullscreen"));

            // Notify individual properties that are still used
            OnPropertyChanged(nameof(TypeComplete));
            OnPropertyChanged(nameof(TypeDifferential));
        }

        /// <summary>
        /// Closes the application.
        /// </summary>
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}