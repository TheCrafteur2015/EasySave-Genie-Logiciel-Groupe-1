# 🛡️ EasySave - Version 3.0

**Solution de gestion de sauvegardes haute performance avec exécution parallèle et contrôle dynamique.** Cette version 3.0 marque une évolution majeure en abandonnant le mode séquentiel pour une architecture multithreadée, permettant l'exécution simultanée des travaux tout en garantissant une gestion fine de la bande passante et des priorités de fichiers.

## ✨ Nouveautés de la Version 3.0

* **Sauvegardes en Parallèle :** Les travaux de sauvegarde s'exécutent désormais de manière concurrente pour optimiser les ressources système.
* **Gestion des Priorités :** Un mécanisme de blocage assure que les fichiers prioritaires sont traités avant tout fichier non prioritaire sur l'ensemble des travaux.
* **Contrôle de Flux :** Limitation du transfert simultané de fichiers volumineux (supérieurs à n Ko, paramétrable) pour éviter la saturation réseau.
* **Interaction Temps Réel :** Interface utilisateur permettant de mettre en **Pause**, **Play** ou d'**Arrêter** chaque travail individuellement ou globalement.
* **Pause Automatique "Métier" :** Détection dynamique du logiciel métier avec mise en pause immédiate et reprise automatique dès la fermeture du processus.
* **Persistance des Paramètres :** Sauvegarde automatique de la configuration (langue, mode de fenêtre, logiciel métier, cryptage) entre les lancements.
* **CryptoSoft Mono-Instance :** Sécurisation via un Mutex système pour garantir une exécution unique et éviter les conflits d'accès.

## 📋 Fonctionnalités Principales

* **Types de Sauvegarde :** Complète et Différentielle.
* **Multi-langue :** Support dynamique et persistant du Français 🇫🇷 et de l'Anglais 🇬🇧.
* **Monitoring Avancé :** * Suivi de progression en temps réel (pourcentage et octets).
    * Logs journaliers exportables en **JSON** ou **XML** incluant les temps de transfert et de cryptage.
* **Gestion Robuste des IDs :** Réorganisation automatique des identifiants des travaux lors d'une suppression pour maintenir une liste cohérente.

## 🚀 Installation et Compilation

### Prérequis Techniques
* **.NET 8.0 SDK**.
* **Extension Avalonia pour Visual Studio 2022**.
* **Logiciel de cryptage CryptoSoft.exe** (inclus et géré par le build automatique).

### Compilation
Pour générer la solution complète (incluant la compilation automatique de CryptoSoft) :
1. Ouvrir un terminal à la racine du projet.
2. Lancer la compilation via le script automatisé :
   `build.bat` (Windows) ou `dotnet build EasySave.sln -c Release`.

## 💻 Mode Console & CLI (Compatibilité)
L'application conserve sa compatibilité en ligne de commande :
* `EasySave.exe 1-3` : Exécute les travaux 1 à 3.
* `EasySave.exe 1;3` : Exécute les travaux 1 et 3.
* **Nouveau :** Intégration d'un tableau de bord interactif pour le monitoring des sauvegardes parallèles.

## 🏗️ Architecture Technique
Le logiciel repose sur une architecture **MVVM** et utilise des mécanismes de synchronisation avancés :
* **Task Parallel Library (TPL) :** Pour la gestion des threads et de l'asynchronisme.
* **Mutex & Sémaphores :** Pour la gestion mono-instance de CryptoSoft et la limitation des transferts volumineux.
* **Design Patterns :** Singleton (Managers), Strategy (Algorithmes de copie), Factory (Instanciation des jobs), Command (Interactions).

## 👥 Auteurs
**Génie-Logiciel - Groupe 1** *CESI Rouen - 3ème année Ingénieur Informatique (FISA)*.
