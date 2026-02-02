using System.Collections.Generic;
using System.Globalization;

namespace EasySave.Views.Localization
{
    /// <summary>
    /// Handles application localization
    /// </summary>
    public class LocalizationService
    {
        private Dictionary<string, Dictionary<string, string>> _translations;
        private string _currentLanguage;

        public LocalizationService()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();
            InitializeTranslations();
            
            // Default to English
            _currentLanguage = "en";
        }

        public void SetLanguage(string languageCode)
        {
            if (_translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
            }
        }

        public string GetString(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && 
                _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            return key;
        }

        private void InitializeTranslations()
        {
            // English translations
            _translations["en"] = new Dictionary<string, string>
            {
                // Menu
                ["menu_title"] = "=== EasySave - Backup Manager ===",
                ["menu_create"] = "1. Create a new backup job",
                ["menu_execute"] = "2. Execute a backup job",
                ["menu_execute_all"] = "3. Execute all backup jobs",
                ["menu_list"] = "4. List all backup jobs",
                ["menu_delete"] = "5. Delete a backup job",
                ["menu_language"] = "6. Change language",
                ["menu_exit"] = "7. Exit",
                ["menu_choice"] = "Enter your choice: ",

                // Job creation
                ["create_name"] = "Enter backup job name: ",
                ["create_source"] = "Enter source directory: ",
                ["create_target"] = "Enter target directory: ",
                ["create_type"] = "Select backup type (1=Complete, 2=Differential): ",
                ["create_success"] = "Backup job created successfully!",
                ["create_failure"] = "Failed to create backup job. Maximum limit reached or invalid data.",

                // Job execution
                ["execute_id"] = "Enter backup job ID to execute: ",
                ["execute_success"] = "Backup job completed successfully!",
                ["execute_failure"] = "Failed to execute backup job: ",
                ["execute_all_start"] = "Executing all backup jobs...",

                // Job listing
                ["list_title"] = "\n=== Backup Jobs ===",
                ["list_empty"] = "No backup jobs found.",
                ["list_id"] = "ID: ",
                ["list_name"] = "Name: ",
                ["list_source"] = "Source: ",
                ["list_target"] = "Target: ",
                ["list_type"] = "Type: ",
                ["list_state"] = "State: ",
                ["list_last_exec"] = "Last Execution: ",

                // Job deletion
                ["delete_id"] = "Enter backup job ID to delete: ",
                ["delete_success"] = "Backup job deleted successfully!",
                ["delete_failure"] = "Failed to delete backup job.",

                // Language
                ["language_select"] = "Select language (1=English, 2=Français): ",
                ["language_changed"] = "Language changed successfully!",

                // Progress
                ["progress_active"] = "Status: Active",
                ["progress_files"] = "Files: {0}/{1}",
                ["progress_size"] = "Size: {0}/{1} bytes",
                ["progress_current"] = "Current file: {0}",
                ["progress_percentage"] = "Progress: {0:F2}%",

                // Common
                ["press_enter"] = "Press Enter to continue...",
                ["invalid_choice"] = "Invalid choice. Please try again.",
                ["error"] = "Error: ",
                ["type_complete"] = "Complete",
                ["type_differential"] = "Differential",
                ["state_active"] = "Active",
                ["state_inactive"] = "Inactive",
                ["state_completed"] = "Completed",
                ["state_error"] = "Error",
                ["never"] = "Never"
            };

            // French translations
            _translations["fr"] = new Dictionary<string, string>
            {
                // Menu
                ["menu_title"] = "=== EasySave - Gestionnaire de Sauvegarde ===",
                ["menu_create"] = "1. Créer un nouveau travail de sauvegarde",
                ["menu_execute"] = "2. Exécuter un travail de sauvegarde",
                ["menu_execute_all"] = "3. Exécuter tous les travaux de sauvegarde",
                ["menu_list"] = "4. Lister tous les travaux de sauvegarde",
                ["menu_delete"] = "5. Supprimer un travail de sauvegarde",
                ["menu_language"] = "6. Changer de langue",
                ["menu_exit"] = "7. Quitter",
                ["menu_choice"] = "Entrez votre choix : ",

                // Job creation
                ["create_name"] = "Entrez le nom du travail de sauvegarde : ",
                ["create_source"] = "Entrez le répertoire source : ",
                ["create_target"] = "Entrez le répertoire cible : ",
                ["create_type"] = "Sélectionnez le type de sauvegarde (1=Complète, 2=Différentielle) : ",
                ["create_success"] = "Travail de sauvegarde créé avec succès !",
                ["create_failure"] = "Échec de la création du travail de sauvegarde. Limite maximale atteinte ou données invalides.",

                // Job execution
                ["execute_id"] = "Entrez l'ID du travail de sauvegarde à exécuter : ",
                ["execute_success"] = "Travail de sauvegarde terminé avec succès !",
                ["execute_failure"] = "Échec de l'exécution du travail de sauvegarde : ",
                ["execute_all_start"] = "Exécution de tous les travaux de sauvegarde...",

                // Job listing
                ["list_title"] = "\n=== Travaux de Sauvegarde ===",
                ["list_empty"] = "Aucun travail de sauvegarde trouvé.",
                ["list_id"] = "ID : ",
                ["list_name"] = "Nom : ",
                ["list_source"] = "Source : ",
                ["list_target"] = "Cible : ",
                ["list_type"] = "Type : ",
                ["list_state"] = "État : ",
                ["list_last_exec"] = "Dernière Exécution : ",

                // Job deletion
                ["delete_id"] = "Entrez l'ID du travail de sauvegarde à supprimer : ",
                ["delete_success"] = "Travail de sauvegarde supprimé avec succès !",
                ["delete_failure"] = "Échec de la suppression du travail de sauvegarde.",

                // Language
                ["language_select"] = "Sélectionnez la langue (1=English, 2=Français) : ",
                ["language_changed"] = "Langue changée avec succès !",

                // Progress
                ["progress_active"] = "État : Actif",
                ["progress_files"] = "Fichiers : {0}/{1}",
                ["progress_size"] = "Taille : {0}/{1} octets",
                ["progress_current"] = "Fichier actuel : {0}",
                ["progress_percentage"] = "Progression : {0:F2}%",

                // Common
                ["press_enter"] = "Appuyez sur Entrée pour continuer...",
                ["invalid_choice"] = "Choix invalide. Veuillez réessayer.",
                ["error"] = "Erreur : ",
                ["type_complete"] = "Complète",
                ["type_differential"] = "Différentielle",
                ["state_active"] = "Actif",
                ["state_inactive"] = "Inactif",
                ["state_completed"] = "Terminé",
                ["state_error"] = "Erreur",
                ["never"] = "Jamais"
            };
        }
    }
}
