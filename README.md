# 🛡️ EasySave V1.1 - Gestionnaire de Sauvegardes

**EasySave** est une solution de sauvegarde de fichiers conçue pour les professionnels. La version 1.1 apporte une mise en conformité stricte avec les exigences de traçabilité (UNC) et une gestion robuste des erreurs de transfert.

---

## ✨ Nouveautés de la V1.1

* **Conformité UNC :** Tous les chemins de fichiers dans les journaux sont désormais convertis automatiquement au format UNC (`\\Hostname\C$\...`) pour une identification unique sur le réseau.
* **Gestion des Erreurs de Transfert :** En cas d'échec (fichier verrouillé, accès refusé), le système consigne désormais une entrée de log spécifique avec un temps d'exécution de `-1ms` pour faciliter le monitoring.
* **Performance Mesurée :** Utilisation de `Stopwatch` pour une précision millimétrée du temps de transfert des fichiers.
* **Flexibilité accrue :** Support du nombre illimité de travaux de sauvegarde via la configuration `-1` dans le fichier `default.json`.

---

## 🚀 Fonctionnalités Clés

* **Modes de Sauvegarde :** * **Complète :** Duplication intégrale des répertoires.
    * **Différentielle :** Seuls les fichiers modifiés ou nouveaux sont copiés, optimisant l'espace disque.
* **Ligne de Commande (CLI) :** Exécution rapide via arguments :
    * `EasySave.exe 1-5` (Intervalle)
    * `EasySave.exe 1;3;6` (Liste spécifique)
* **Multilingue :** Support natif du Français et de l'Anglais via fichiers de ressources JSON.

---

## 🏗️ Architecture & Qualité

Le projet suit les principes du **Génie Logiciel** avec l'implémentation de plusieurs Design Patterns :
* **Strategy :** Isolation des algorithmes de sauvegarde (Complete vs Differential).
* **Factory :** Création dynamique des stratégies et des loggers (JSON/XML/Text).
* **Singleton :** Instance unique pour le `BackupManager` et le moteur de traduction `I18n`.
* **Command :** Découplage des actions utilisateur et de la logique métier (MVVM).

---

## 💻 Installation

### Prérequis
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Compilation
Utilisez les scripts automatisés à la racine :
* **Windows :** `build.bat`
* **Linux/macOS :** `./build.sh`

---

## 📁 Structure des Données

Les données sont isolées dans le répertoire `AppData` pour respecter les standards OS :
`%APPDATA%\EasySave\`

* **\Config :** `backups.json` (Liste des travaux) et `config.json`.
* **\Logs :** Journaux quotidiens (`yyyy-MM-dd.log`) aux formats JSON/XML/Texte.
* **\State :** `state.json` (État d'avancement temps réel pour les moniteurs externes).

---

## 👥 Équipe
* **Groupe 1** - CESI Rouen
* Cursus Ingénieur Informatique (3ème année)