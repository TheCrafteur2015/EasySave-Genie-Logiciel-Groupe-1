# EasySave - Version 2.0

**Projet de programmation système - Cesi École d'Ingénieurs (Groupe 1)**

EasySave est une solution logicielle de sauvegarde développée pour l'entreprise **ProSoft**. La version 2.0 marque une évolution majeure du produit avec le passage d'une interface en ligne de commande vers une **Interface Graphique (WPF)** moderne et ergonomique. Elle intègre également des fonctionnalités avancées de sécurité (chiffrement) et de contrôle métier.

---

## 🚀 Nouveautés de la version 2.0

Cette version transforme l'expérience utilisateur et renforce la sécurité des données :

* **Interface Graphique (GUI) :** Abandon de la console au profit d'une interface WPF intuitive respectant le pattern MVVM.
* **Chiffrement des données :** Intégration du module **CryptoSoft** pour chiffrer les fichiers sensibles (extensions configurables).
* **Travaux illimités :** La limite de 5 travaux de sauvegarde est supprimée. Vous pouvez désormais créer autant de travaux que nécessaire.
* **Protection Métier :** Le logiciel détecte l'exécution de logiciels métiers critiques (ex: Calculatrice, SAP) et empêche/suspend la sauvegarde pour garantir l'intégrité des données.
* **Logs enrichis :** Ajout du temps de chiffrement dans les logs et maintien du choix de format (JSON/XML).

---

## 📋 Fonctionnalités Principales

### Gestion des Sauvegardes
* **Création/Modification :** Interface visuelle pour configurer le Nom, la Source, la Cible et le Type.
* **Types supportés :**
    * *Complète :* Sauvegarde intégrale de l'arborescence.
    * *Différentielle :* Sauvegarde uniquement des fichiers modifiés depuis la dernière complète.
* **Exécution :** Lancement unitaire ou séquentiel de l'ensemble des travaux via l'interface.

### Sécurité & Paramétrage
* **Chiffrement via CryptoSoft :** L'utilisateur définit une liste d'extensions (ex: `.txt`, `.docx`) dans les paramètres. Les fichiers correspondants sont chiffrés avant copie.
* **Logiciel Métier :** Configuration du nom du processus métier (ex: `CalculatorApp`). Si ce processus est actif, EasySave refuse le lancement ou arrête proprement le travail en cours.

### Monitoring & Logs
* **État en temps réel :** Affichage de la progression (Barre de progression, %, fichier en cours) directement dans l'IHM et écriture dans `state.json`.
* **Journaux d'activité :** Génération de logs journaliers via la librairie **EasyLog.dll**.
    * Support des formats **JSON** et **XML**.
    * Donnée ajoutée : Temps de cryptage (en ms).

---

## 🛠 Prérequis Techniques

* **Système d'exploitation :** Windows 10/11 (x64) recommandé pour le support WPF.
* **Framework :** .NET 8.0 Desktop Runtime.
* **Module Externe :** `CryptoSoft.exe` doit être présent à la racine ou dans le chemin configuré.
* **Droits :** Droits d'écriture/lecture sur les répertoires sources/cibles et le dossier `%AppData%`.

---

## 📦 Installation et Déploiement

### Depuis les sources
1.  **Cloner le dépôt :**
    ```bash
    git clone <url_du_repo>
    cd EasySave
    ```
2.  **Restaurer et Compiler :**
    Ouvrez la solution `EasySave.sln` dans **Visual Studio 2022**.
    Générez la solution (Build Solution) en mode `Release`.
3.  **CryptoSoft :**
    Assurez-vous que l'exécutable `CryptoSoft.exe` est copié dans le dossier de sortie (`/bin/Release/net8.0-windows/`).

### Structure des dossiers
L'application crée automatiquement son environnement de travail dans `%AppData%\EasySave\` :
* `Config/` : Contient `settings.json` (Langue, Format Log, Extensions Crypto, Logiciel Métier).
* `Jobs/` : Contient la sérialisation des travaux de sauvegarde.
* `Logs/` : Historique des journaux.

---

## 💻 Guide d'Utilisation

### Interface Utilisateur (WPF)
L'application se découpe en plusieurs onglets :

1.  **Accueil (Dashboard) :** Vue d'ensemble des travaux, état actuel et boutons de lancement rapide.
2.  **Gestion des Travaux :**
    * Formulaire pour ajouter un travail (Nom, Source, Cible, Type).
    * Liste déroulante ou grille pour modifier/supprimer des travaux existants.
3.  **Exécution :**
    * Sélectionnez un ou plusieurs travaux (via des cases à cocher).
    * Cliquez sur **"Exécuter"**. Une barre de progression indique l'avancement global.
4.  **Paramètres :**
    * **Langue :** Basculer entre Français et Anglais.
    * **Format Log :** Choisir XML ou JSON.
    * **Extensions à chiffrer :** Saisir les extensions (ex: `.pdf;.txt`).
    * **Logiciel Métier :** Saisir le nom du processus à surveiller.

### Mode Ligne de Commande (Compatibilité)
Bien que graphique, l'application conserve une compatibilité avec les arguments de la v1.0 pour l'intégration dans des scripts :
* `EasySave.exe 1-3` : Lance l'interface et démarre automatiquement les travaux 1 à 3.
* `EasySave.exe 1;5` : Lance l'interface et démarre les travaux 1 et 5.

---

## ⚙️ Détails des Logs et États

### Fichier Log Journalier
Le fichier log contient désormais une entrée spécifique pour le chiffrement :
* `EncryptionTime` :
    * `0` : Pas de chiffrement.
    * `> 0` : Temps en ms (cryptage réussi).
    * `< 0` : Code erreur (échec CryptoSoft).

### Interdiction Métier
Si le logiciel métier est détecté lors d'une tentative de sauvegarde, l'événement est consigné dans le log journalier et une notification visuelle apparaît dans l'interface (Popup ou message d'état).

---

## 🏗 Architecture Technique

Le projet repose sur l'architecture **MVVM (Model-View-ViewModel)** pour garantir la maintenabilité et la séparation des responsabilités.

* **Model :**
    * Contient la logique métier pure (Copie de fichier, Appel à CryptoSoft, Gestion des I/O).
    * Classes : `BackupJob`, `BackupService`, `LogService`.
* **ViewModel :**
    * Fait le lien entre la Vue et le Modèle. Il expose les données via `INotifyPropertyChanged` et gère les actions utilisateur via des `ICommand`.
    * Classes : `MainViewModel`, `SettingsViewModel`, `JobViewModel`.
* **View :**
    * Interface utilisateur définie en XAML (Windows Presentation Foundation).
    * Aucun code métier dans le "Code-Behind" (`.xaml.cs`).
* **Dépendances :**
    * `EasyLog.dll` : Gestionnaire de logs (projet externe réutilisé).
    * `Newtonsoft.Json` : Pour la sérialisation des configurations.

---

## 👥 Auteurs

**Groupe 1 - CESI Rouen**
Projet réalisé dans le cadre du bloc "Programmation Système / Interface Graphique".
Code source développé pour l'entité **ProSoft**.