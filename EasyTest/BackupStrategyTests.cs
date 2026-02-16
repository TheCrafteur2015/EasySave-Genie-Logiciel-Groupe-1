using EasySave.Backup;
using System.Diagnostics;

namespace EasyTest
{
    [TestClass]
    public class BackupStrategyTests
    {
        private string _dossierSource = null!;
        private string _dossierCible = null!;
        private const string NomFichierTest = "fichier_test.txt";
        private const string NomFichierCrypto = "secret.txt";

        [TestInitialize]
        public void Setup()
        {
            _dossierSource = Path.Combine(Path.GetTempPath(), "EasySave_Source_Test");
            _dossierCible = Path.Combine(Path.GetTempPath(), "EasySave_Cible_Test");

            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);

            Directory.CreateDirectory(_dossierSource);

            var bm = BackupManager.GetBM();
            foreach (var job in bm.GetAllJobs()) bm.DeleteJob(job.Id);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);
        }

        [TestMethod]
        public void TestSauvegardeComplete_CopieFichiers()
        {
            File.WriteAllText(Path.Combine(_dossierSource, NomFichierTest), "Contenu de test");
            var bm = BackupManager.GetBM();
            bm.AddJob("TestComplet", _dossierSource, _dossierCible, BackupType.Complete);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJob(id);

            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            Assert.IsTrue(File.Exists(fichierCible), "Le fichier aurait dû être copié dans le dossier cible.");
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

            Thread.Sleep(1000);
            File.WriteAllText(fichierSource, "Version 2 - Modifié");

            bm.ExecuteJob(id);
            DateTime dateDeuxiemeCopie = File.GetLastWriteTime(fichierCible);

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie, "Le fichier cible aurait dû être mis à jour.");
            Assert.AreEqual("Version 2 - Modifié", File.ReadAllText(fichierCible));
        }

        [TestMethod]
        public void TestLogicielMetier_Blocage()
        {
            Process p = Process.Start("calc.exe");
            try
            {
                Thread.Sleep(2000);

                var bm = BackupManager.GetBM();
                bm.AddJob("TestBlocage", _dossierSource, _dossierCible, BackupType.Complete);
                int id = bm.GetAllJobs()[0].Id;

                bool succes = bm.ExecuteJob(id);

                Assert.IsFalse(succes, "Le job aurait dû échouer (retourner false) car la Calculatrice est ouverte.");
            }
            finally
            {
                if (p != null && !p.HasExited) p.Kill();
            }
        }

        [TestMethod]
        public void TestCryptoSoft_Integration()
        {
            string cheminFichierSource = Path.Combine(_dossierSource, NomFichierCrypto);
            string contenuClair = "Ceci est un secret";
            File.WriteAllText(cheminFichierSource, contenuClair);

            var bm = BackupManager.GetBM();
            bm.AddJob("TestCrypto", _dossierSource, _dossierCible, BackupType.Complete);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJob(id);

            string cheminFichierCible = Path.Combine(_dossierCible, NomFichierCrypto);
            Assert.IsTrue(File.Exists(cheminFichierCible), "Le fichier crypté doit exister.");

            string contenuCible = File.ReadAllText(cheminFichierCible);

            Assert.AreNotEqual(contenuClair, contenuCible, "Le contenu du fichier cible devrait être crypté (différent de la source).");
        }
    }
}