# 🛡️ EasySave - Version 1.1

**Solution de gestion de sauvegardes professionnelle** *Projet de programmation système - Cesi École d'Ingénieurs (Groupe 1)*

EasySave est une application console robuste conçue pour automatiser et sécuriser vos travaux de sauvegarde. La version 1.1 introduit une flexibilité accrue pour les administrateurs système et une mise en conformité stricte des journaux d'activité.

---

## ✨ Nouveautés de la Version 1.1

* **Format de Log Configurable :** L'utilisateur peut choisir entre les formats **JSON**, **XML** ou **Texte** via la clé `LoggerFormat` dans le fichier de configuration.
* **Conformité UNC :** Les chemins de fichiers dans les logs sont convertis au format UNC (`\\Hostname\C$\...`) pour une traçabilité réseau optimale.
* **Gestion des Erreurs de Transfert :** En cas d'échec de copie, une entrée est générée avec un temps d'exécution de `-1ms` pour signaler l'anomalie.
* **Travaux Illimités :** Possibilité de désactiver la limite de 5 travaux en réglant `UseBackupJobLimit` à `false` dans la configuration.

---

## 📋 Fonctionnalités Principales

* **Modes de Sauvegarde :**
    * **Complète :** Copie l'intégralité des répertoires sources vers la destination.
    * **Différentielle :** Optimise l'espace en ne copiant que les fichiers modifiés ou nouveaux depuis la dernière exécution.
* **Modes d'Exécution :**
    * **Interactif :** Menu complet avec gestion des erreurs de saisie et localisation en temps réel.
    * **Ligne de Commande (CLI) :** Support des intervalles (`1-3`), des listes (`1;3;5`) ou des IDs uniques (`2`).
* **Monitoring Temps Réel :** Un fichier `state.json` est mis à jour dynamiquement pour suivre l'avancement (fichiers restants, pourcentage, taille totale).
* **Multilingue :** Support complet du **Français** et de l'**Anglais** (extensible via fichiers JSON).

---

## 🏗️ Architecture Logicielle (MVVM)

Le projet utilise une architecture inspirée du pattern **MVVM** pour séparer la logique métier de l'interface utilisateur :

* **Model :** Les entités de données (`BackupJob`) et les stratégies de copie.
* **View :** L'interface console (`ConsoleView`) gérant les interactions.
* **ViewModel :** Le `BackupManager` (Singleton) qui orchestre l'exécution et la persistance.

### Design Patterns Implémentés :
* **Strategy :** Pour isoler les algorithmes de sauvegarde (`IBackupStrategy`).
* **Factory :** Pour l'instanciation dynamique des stratégies et des types de loggers.
* **Command :** Pour encapsuler les actions du menu et faciliter l'extension des fonctionnalités.
* **Singleton :** Pour garantir l'unicité du `BackupManager` et du moteur `I18n`.

---

## 🚀 Installation et Compilation

### Prérequis
* **.NET 8.0 SDK**

### Build
Utilisez les scripts automatisés à la racine du dépôt :
* **Windows :** Lancer `build.bat`
* **Linux / macOS :** Lancer `build.sh`

Les binaires compilés pour chaque plateforme seront disponibles dans le dossier `./publish/`.

---

## ⚙️ Configuration & Logs

L'application stocke ses paramètres et journaux dans le répertoire `AppData` de l'utilisateur :
`%APPDATA%\EasySave\`

* **\Config :** Contient `backups.json` (liste des jobs) et `config.json` (paramètres globaux).
* **\Logs :** Journaux quotidiens nommés par date (ex: `2026-02-13.json`).
* **\State :** État d'avancement en temps réel dans `state.json`.

---

## 🛠️ Organisation du Dépôt

* **EasySave :** Logique métier, stratégies de sauvegarde et gestionnaires.
* **EasyConsole :** Point d'entrée de l'application et interface utilisateur.
* **EasyLog :** Bibliothèque partagée pour la gestion des logs multi-formats (JSON/XML/Texte).
* **EasyTest :** Tests unitaires validant la sérialisation et les fonctionnalités critiques.

---

## 👥 Auteurs
**Groupe 1 - CESI Rouen** *Ingénieur Informatique - 3ème année*