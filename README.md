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