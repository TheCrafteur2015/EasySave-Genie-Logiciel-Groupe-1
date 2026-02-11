# EasySave - Version 1.1

**Projet de programmation système - Cesi École d'Ingénieurs (Groupe 1)**

EasySave est un logiciel de gestion de sauvegarde développé pour l'entreprise **ProSoft**. Cette application console permet de configurer et d'exécuter des travaux de sauvegarde de manière séquentielle, tout en assurant un suivi précis via des journaux d'activité (Logs) et un état en temps réel.

## 🚀 Nouveautés de la version 1.1

Cette version introduit une fonctionnalité majeure demandée par les clients tout en conservant la stabilité de la version 1.0 :
* **Choix du format des Logs :** L'utilisateur peut désormais configurer le format des fichiers journaux journaliers en **JSON** ou en **XML** via le fichier de configuration.

## 📋 Fonctionnalités Principales

* **Mode Console :** Interface textuelle légère et performante.
* **Multilingue :** Support complet du **Français** et de l'**Anglais**.
* **Travaux de sauvegarde :** Gestion jusqu'à **5 travaux** de sauvegarde configurables.
* **Types de sauvegarde :**
    * *Complète* : Copie intégrale des fichiers sources.
    * *Différentielle* : Copie uniquement des fichiers modifiés depuis la dernière sauvegarde.
* **Exécution :**
    * Lancement individuel d'un travail.
    * Exécution séquentielle de tous les travaux ou d'une sélection personnalisée.
* **Monitoring :**
    * Fichier d'état en temps réel (`state.json`) pour suivre la progression.
    * Fichier de Log journalier (Format configurable : JSON ou XML) géré par la bibliothèque `EasyLog`.

## 🛠 Prérequis Techniques

* **Système d'exploitation :** Windows (x64), Linux (x64) ou macOS (x64).
* **Framework :** .NET 8.0 SDK ou Runtime.
* **Droits :** Droits d'écriture requis sur les dossiers source, cible et le dossier de configuration (`AppData` ou équivalent).

## 📦 Installation et Compilation

Le projet fournit des scripts automatisés pour la compilation et le déploiement.

### Depuis les sources

1.  Clonez le dépôt :
    ```bash
    git clone <url_du_repo>
    cd EasySave
    ```

2.  Utilisez le script de build correspondant à votre OS :
    * **Windows** : Exécutez `build.bat` depuis l'invite de commande.
    * **Linux / macOS** : Exécutez `build.sh` (assurez-vous que le script est exécutable : `chmod +x build.sh`).

3.  Les binaires seront générés dans le dossier `./publish/`.

## 💻 Utilisation

### Mode Interactif (Menu)
Lancez l'exécutable `EasySave.exe` (ou `./EasySave`) pour accéder au menu principal :

1.  **Créer un travail :** Définir le nom, la source, la cible et le type (Complet/Différentiel).
2.  **Exécuter un travail :** Lancer une sauvegarde spécifique par son ID.
3.  **Exécuter tout :** Lancer tous les travaux séquentiellement.
4.  **Lister les travaux :** Voir la configuration actuelle des travaux.
5.  **Supprimer un travail :** Retirer une configuration existante.
6.  **Langue :** Basculer l'interface entre Français et Anglais.
7.  **Quitter**

### Mode Ligne de Commande
EasySave peut être piloté via des arguments au lancement pour l'automatisation (tâches planifiées, scripts) :

* **Sauvegarde unique (ID) :**
    ```bash
    EasySave.exe 1
    ```
* **Plage de sauvegardes (Range) :**
    ```bash
    EasySave.exe 1-3
    # Exécute les travaux 1, 2 et 3 à la suite
    ```
* **Liste de sauvegardes (List) :**
    ```bash
    EasySave.exe 1;3;5
    # Exécute uniquement les travaux 1, 3 et 5
    ```