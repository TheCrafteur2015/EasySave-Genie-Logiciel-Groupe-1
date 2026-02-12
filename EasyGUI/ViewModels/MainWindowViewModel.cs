using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Backup;
using System.Collections.ObjectModel;
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

        public ICommand SwitchThemeCommand { get; }

        public MainWindowViewModel()
        {
            // 1. On récupère ton instance unique
            _backupManager = BackupManager.GetBM();

            // 2. On remplit la liste pour l'interface graphique
            // On charge les jobs existants depuis ton manager
            var jobsFromManager = _backupManager.GetAllJobs();
            BackupJobs = new ObservableCollection<BackupJob>(jobsFromManager);

            SwitchThemeCommand = new RelayCommand<string>(SwitchTheme);
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
                // (Ceci est une version simplifiée, idéalement on ajoute juste l'élément)
                BackupJobs.Clear();
                foreach (var job in _backupManager.GetAllJobs())
                {
                    BackupJobs.Add(job);
                }
            }
        }
    }
}