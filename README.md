# 🛡️ EasySave - Version 2.0

**Solution de gestion de sauvegardes professionnelle avec interface graphique** Cette version marque le passage à une interface utilisateur moderne utilisant le framework **Avalonia** et intègre le logiciel de cryptage **CryptoSoft**.

---

## ✨ Nouveautés de la Version 2.0

* **Interface Graphique :** Une toute nouvelle expérience utilisateur développée sous le framework **Avalonia**.
* **Cryptage CryptoSoft :** Intégration de l'outil de chiffrement pour sécuriser les données sensibles selon les extensions configurées.
* **Travaux illimités :** Suppression de la limite des 5 travaux de sauvegarde.
* **Détection de Processus :** Le système surveille les **processus** métiers définis et suspend automatiquement les sauvegardes si l'un d'eux est détecté.

---

## 📋 Fonctionnalités Principales

* **Types de Sauvegarde :** Complète et Différentielle.
* **Multi-langue :** Support dynamique du Français et de l'Anglais.
* **Monitoring & Logs :** * Génération de logs journaliers aux formats JSON ou XML incluant les temps de cryptage.
    * **Note :** L'affichage de la progression n'est pas disponible dans cette version.

---

## 🚀 Installation et Compilation

### Prérequis Techniques
* .NET 8.0 SDK.
* Logiciel de cryptage **CryptoSoft.exe** présent dans le répertoire configuré.

### Depuis les sources
Pour compiler le projet en mode **Release** (optimisé pour l'exécution finale) :

1. Accéder au dossier du projet complet :  
   `cd EasySave.Desktop` (ou le nom exact de votre dossier projet)
2. Lancer la compilation :  
   `dotnet build EasySave.sln -c Release`

---

## 💻 Mode Console (Compatibilité)
L'application conserve une compatibilité ascendante pour les utilisateurs souhaitant piloter les sauvegardes via un terminal.

---

## 🏗️ Architecture Technique
Le logiciel est structuré autour du pattern **MVVM** pour garantir une séparation claire entre l'interface Avalonia et la logique métier de sauvegarde. L'ensemble est conçu pour être évolutif et faciliter la maintenance à long terme.

---

## 👥 Auteurs
**Groupe 1 - CESI Rouen** *Projet de Programmation Système - 3ème année Ingénieur Informatique.*