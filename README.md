EasySave v2.0 - ProSoft Solutions
📝 Présentation du Projet
EasySave est une solution logicielle de sauvegarde de données conçue pour les entreprises. La version 2.0 marque une évolution majeure en passant d'une interface console à une Interface Graphique (GUI) moderne, tout en intégrant des fonctionnalités avancées de sécurité et de contrôle métier.

✨ Nouvelles Fonctionnalités (v2.0)
Par rapport à la version initiale, la version 2.0 apporte les améliorations suivantes :

Interface Graphique (WPF) : Migration complète vers une interface utilisateur intuitive basée sur le Framework WPF.

Travaux Illimités : Suppression de la limite des 5 travaux ; l'utilisateur peut désormais configurer un nombre infini de sauvegardes.

Chiffrement avec CryptoSoft : Intégration du logiciel externe CryptoSoft pour chiffrer les fichiers sensibles (extensions configurables).

Détection de Logiciel Métier : Suspension automatique des sauvegardes si un logiciel spécifique (ex: Calculatrice, SAP, etc.) est détecté en cours d'exécution.

Logs Multi-formats : Possibilité de choisir entre le format JSON et XML pour les journaux d'activité.

Interopérabilité : Maintien de la compatibilité avec les commandes CLI de la version 1.0.

🛠 Spécifications Techniques
Environnement de Développement
IDE : Visual Studio 2022

Langage : C# 12.0

Framework : .NET 8.0

Architecture : MVVM (Model-View-ViewModel) pour une séparation stricte entre l'interface et la logique.

Librairies : Utilisation de la DLL EasyLog.dll pour la gestion des logs.

Installation & Support
Emplacement par défaut : %ProgramFiles%\ProSoft\EasySaveV2\

Configuration minimale : Windows 10/11, .NET 8.0 Runtime.

Fichiers de configuration : Situés dans %AppData%\EasySave\, format JSON.