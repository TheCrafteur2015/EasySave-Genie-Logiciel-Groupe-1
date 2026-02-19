# 🛡️ EasySave - Logiciel de Gestion de Sauvegardes

EasySave est une application console conçue pour automatiser et sécuriser vos travaux de sauvegarde. Développée en C# (.NET 8.0), elle utilise une architecture modulaire pour offrir une solution flexible, multilingue et performante.

## ✨ Fonctionnalités

* **Types de Sauvegarde :**
    * **Complète :** Copie l'intégralité des fichiers sources vers la destination.
    * **Différentielle :** Copie uniquement les fichiers modifiés ou nouveaux depuis la dernière sauvegarde.
* **Interface Bilingue :** Support complet du Français 🇫🇷 et de l'Anglais 🇬🇧.
* **Suivi en Temps Réel :** État d'avancement des travaux (pourcentage, fichiers restants, taille) exporté dynamiquement dans un fichier `state.json`.
* **Système de Logs Flexible :** Génération de journaux quotidiens, le format texte est par défaut.
* **Mode Commande :** Exécution via ligne de commande (ID unique, liste `;` ou intervalle `-`).

## 🏗️ Architecture Technique

Le projet repose sur une architecture inspirée du pattern MVVM et implémente plusieurs Design Patterns pour garantir la qualité logicielle :
* **Singleton :** Utilisé pour le `BackupManager` et le système de localisation `I18n`.
* **Strategy :** Pour isoler la logique des algorithmes de sauvegarde (`Complete` vs `Differential`).
* **Factory :** Pour l'instanciation dynamique des stratégies via `BackupStrategyFactory`.
* **Command :** Pour encapsuler les actions utilisateur dans l'interface console.

## 🚀 Installation & Build

### Prérequis
* .NET 8.0 SDK.

### Compilation
Des scripts d'automatisation sont fournis à la racine du dépôt :
* **Windows :** Exécuter `build.bat`.
* **Linux/macOS :** Exécuter `build.sh`. Les binaires seront générés dans le dossier `./publish/`.

## ⚙️ Configuration & Stockage

L'application centralise ses données dans le répertoire AppData de l'utilisateur : `%APPDATA%\EasySave\`.

| Emplacement | Contenu |
| :--- | :--- |
| `/Config` | Configuration globale (`config.json`) et liste des jobs (`backups.json`). |
| `/Logs` | Journaux quotidiens des transferts nommés par date. |
| `/State` | État d'avancement temps réel stocké dans `state.json`. |

## 🛠️ Organisation du Dépôt

* **EasySave :** Projet principal contenant la logique métier, les modèles et la vue console.
* **EasyLog :** Librairie dédiée à la journalisation et à la gestion des différents formats de sortie.
* **EasyTest :** Suite de tests unitaires pour valider les composants critiques.

## 👥 Auteurs

Génie-Logiciel - Groupe 1 CESI Rouen - 3ème année de cursus Ingénieur Informatique.
