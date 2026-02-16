using EasySave.Backup;
using System.Diagnostics;

namespace EasyTest
{
    /// <summary>
    /// Unit tests for backup strategies (Complete and Differential).
    /// Validates file copying, business software detection, and encryption integration.
    /// </summary>
    [TestClass]
    public class BackupStrategyTests
    {
        /// <summary>
        /// Path to the temporary source directory used for testing.
        /// </summary>
        private string _dossierSource = null!;

        /// <summary>
        /// Path to the temporary target directory used for testing.
        /// </summary>
        private string _dossierCible = null!;

        /// <summary>
        /// Constant name for a standard test file.
        /// </summary>
        private const string NomFichierTest = "fichier_test.txt";

        /// <summary>
        /// Constant name for a file that should trigger encryption.
        /// </summary>
        private const string NomFichierCrypto = "secret.txt";

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <remarks>
        /// Creates clean temporary directories and removes any existing backup jobs 
        /// from the BackupManager to prevent test interference.
        /// </remarks>
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

        /// <summary>
        /// Cleans up the test environment after each test.
        /// </summary>
        /// <remarks>
        /// Deletes the temporary source and target directories created during the setup.
        /// </remarks>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);
        }

        /// <summary>
        /// Verifies that a Complete Backup correctly copies files to the target directory.
        /// </summary>
        [TestMethod]
        public void TestSauvegardeComplete_CopieFichiers()
        {
            File.WriteAllText(Path.Combine(_dossierSource, NomFichierTest), "Contenu de test");
            var bm = BackupManager.GetBM();
            bm.AddJob("TestComplet", _dossierSource, _dossierCible, BackupType.Complete);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJob(id);

            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            Assert.IsTrue(File.Exists(fichierCible), "The file should have been copied to the target directory.");
            Assert.AreEqual("Contenu de test", File.ReadAllText(fichierCible));
        }

        /// <summary>
        /// Verifies that a Differential Backup updates a file when it has been modified.
        /// </summary>
        /// <remarks>
        /// This test performs an initial backup, modifies the source file after a delay, 
        /// and verifies that the second backup updates the target file timestamp and content.
        /// </remarks>
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

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie, "The target file should have been updated.");
            Assert.AreEqual("Version 2 - Modifié", File.ReadAllText(fichierCible));
        }

        /// <summary>
        /// Tests the blocking mechanism when business software is detected.
        /// </summary>
        /// <remarks>
        /// Simulates the presence of business software (calc.exe) and verifies 
        /// that the BackupManager refuses to execute the job.
        /// </remarks>
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

                Assert.IsFalse(succes, "The job should have failed (returned false) because Calculator is open.");
            }
            finally
            {
                if (p != null && !p.HasExited) p.Kill();
            }
        }

        /// <summary>
        /// Verifies the integration with CryptoSoft for encrypted backups.
        /// </summary>
        /// <remarks>
        /// Checks that a file identified for encryption is different in the target 
        /// directory compared to the original clear-text source.
        /// </remarks>
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
            Assert.IsTrue(File.Exists(cheminFichierCible), "The encrypted file must exist.");

            string contenuCible = File.ReadAllText(cheminFichierCible);

            Assert.AreNotEqual(contenuClair, contenuCible, "Target file content should be encrypted (different from source).");
        }
    }
}