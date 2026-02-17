using EasySave.Backup;
using EasySave.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace EasyTest
{
    [TestClass]
    public class BackupStrategyTests
    {
        private string _dossierSource = null!;
        private string _dossierCible = null!;

        // On garde les extensions cohérentes avec les tests de priorité
        private const string NomFichierTest = "fichier_test.txt";
        private const string NomFichierCrypto = "secret.txt";

        [TestInitialize]
        public void Setup()
        {
            KillCalculator(); // On s'assure qu'aucun résidu de test précédent ne bloque
            _dossierSource = Path.Combine(Path.GetTempPath(), "EasySave_Source_Test");
            _dossierCible = Path.Combine(Path.GetTempPath(), "EasySave_Cible_Test");

            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);

            Directory.CreateDirectory(_dossierSource);

            // Reset du Singleton pour repartir sur une config propre
            var bm = BackupManager.GetBM();
            foreach (var job in bm.GetAllJobs()) bm.DeleteJob(job.Id);
        }

        [TestCleanup]
        public void Cleanup()
        {
            KillCalculator();
            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);
        }

        /// <summary>
        /// Helper pour nettoyer les processus qui bloquent les tests.
        /// </summary>
        private void KillCalculator()
        {
            var processNames = new[] { "CalculatorApp", "calc", "Calculator" };
            foreach (var name in processNames)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); p.WaitForExit(1000); } catch { }
                }
            }
            Thread.Sleep(100);
        }

        [TestMethod]
        public void TestSauvegardeComplete_CopieFichiers()
        {
            string cheminFichierSource = Path.Combine(_dossierSource, NomFichierTest);
            File.WriteAllText(cheminFichierSource, "Contenu de test");

            var bm = BackupManager.GetBM();
            bm.AddJob("TestComplet", _dossierSource, _dossierCible, BackupType.Complete);
            var job = bm.GetAllJobs()[0];

            bool succes = bm.ExecuteJob(job.Id);

            Assert.IsTrue(succes, "Le job devrait réussir.");
            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            Assert.IsTrue(File.Exists(fichierCible), "Le fichier devrait être présent dans la cible.");
            Assert.AreEqual("Contenu de test", File.ReadAllText(fichierCible));
        }

        [TestMethod]
        public void TestSauvegardeDifferentielle_FichierModifie()
        {
            string fichierSource = Path.Combine(_dossierSource, NomFichierTest);
            File.WriteAllText(fichierSource, "Version 1");

            var bm = BackupManager.GetBM();
            bm.AddJob("TestDiff", _dossierSource, _dossierCible, BackupType.Differential);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJob(id);
            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            DateTime datePremiereCopie = File.GetLastWriteTime(fichierCible);

            Thread.Sleep(1100); // On attend pour être sûr que le timestamp change
            File.WriteAllText(fichierSource, "Version 2 - Modifié");

            bm.ExecuteJob(id);
            DateTime dateDeuxiemeCopie = File.GetLastWriteTime(fichierCible);

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie, "Le fichier cible devrait avoir été mis à jour.");
            Assert.AreEqual("Version 2 - Modifié", File.ReadAllText(fichierCible));
        }

        [TestMethod]
        public void TestLogicielMetier_Blocage()
        {
            Process p = Process.Start("calc.exe");
            try
            {
                Thread.Sleep(2000); // Laisse le temps au process de démarrer
                var bm = BackupManager.GetBM();
                bm.AddJob("TestBlocage", _dossierSource, _dossierCible, BackupType.Complete);
                int id = bm.GetAllJobs()[0].Id;

                bool succes = bm.ExecuteJob(id);
                Assert.IsFalse(succes, "Le job devrait échouer car la calculatrice est ouverte.");
            }
            finally
            {
                if (p != null && !p.HasExited) p.Kill();
            }
        }

        [TestMethod]
        public void TestCryptoSoft_Integration()
        {
            // Setup d'une config temporaire avec un chemin vers CryptoSoft
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configPath = Path.Combine(appData, "Config", "config.json");

            var configSpeciale = new {
                PriorityExtensions = new[] { ".txt" },
                CryptoKey = "1234",
                CryptoSoftPath = @"C:\Path\To\CryptoSoft.exe" // Ajuste ce chemin pour tes tests locaux
            };
            File.WriteAllText(configPath, JsonConvert.SerializeObject(configSpeciale));

            // Force le rechargement du Singleton
            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string cheminFichierSource = Path.Combine(_dossierSource, NomFichierCrypto);
            string contenuClair = "Ceci est un secret";
            File.WriteAllText(cheminFichierSource, contenuClair);

            var bm = BackupManager.GetBM();
            bm.AddJob("TestCrypto", _dossierSource, _dossierCible, BackupType.Complete);
            bm.ExecuteJob(bm.GetAllJobs()[0].Id);

            string cheminFichierCible = Path.Combine(_dossierCible, NomFichierCrypto);
            Assert.IsTrue(File.Exists(cheminFichierCible), "Le fichier crypté doit exister.");
            Assert.AreNotEqual(contenuClair, File.ReadAllText(cheminFichierCible), "Le contenu devrait être chiffré.");
        }

        [TestMethod]
        public void TestPriorite_Differentiel_Blocage()
        {
            // Teste si les fichiers .txt passent bien avant les autres
            var bm = BackupManager.GetBM();
            // ...