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
        private const string NomFichierTest = "fichier_test.dat";
        private const string NomFichierCrypto = "secret.txt";

        [TestInitialize]
        public void Setup()
        {
            KillCalculator();
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
            KillCalculator();
            if (Directory.Exists(_dossierSource)) Directory.Delete(_dossierSource, true);
            if (Directory.Exists(_dossierCible)) Directory.Delete(_dossierCible, true);
        }

        private void KillCalculator()
        {
            var processNames = new[] { "CalculatorApp", "calc", "Calculator" };
            foreach (var name in processNames)
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var p in processes)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1000);
                    }
                    catch { }
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

            Assert.IsTrue(succes);
            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            Assert.IsTrue(File.Exists(fichierCible));
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

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie);
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
                Assert.IsFalse(succes);
            }
            finally
            {
                if (p != null && !p.HasExited) p.Kill();
            }
        }

        [TestMethod]
        public void TestCryptoSoft_Integration()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");


            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                PriorityExtensions = new[] { ".txt" },
                CryptoKey = "1234",
                CryptoSoftPath = @"C:\FISA_A3\Bloc_Génie_Logiciel\EasySave-Genie-Logiciel-Groupe-1\EasyConsole\bin\Debug\net8.0\CryptoSoft.exe"
            };
            File.WriteAllText(configPath, JsonConvert.SerializeObject(configSpeciale));

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string cheminFichierSource = Path.Combine(_dossierSource, NomFichierCrypto);
            string contenuClair = "Ceci est un secret";
            File.WriteAllText(cheminFichierSource, contenuClair);

            var bm = BackupManager.GetBM();
            bm.AddJob("TestCrypto", _dossierSource, _dossierCible, BackupType.Complete);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJob(id);

            string cheminFichierCible = Path.Combine(_dossierCible, NomFichierCrypto);
            Assert.IsTrue(File.Exists(cheminFichierCible), "Le fichier crypté doit exister");

            string contenuCible = File.ReadAllText(cheminFichierCible);

            Assert.AreNotEqual(contenuClair, contenuCible, "Le fichier cible devrait être crypté (différent de la source)");
        }

        [TestMethod]
        public void TestPriorite_Differentiel_Blocage()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");

            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                PriorityExtensions = new[] { ".txt" },
                CryptoKey = "1234",
                CryptoSoftPath = ""
            };
            File.WriteAllText(configPath, JsonConvert.SerializeObject(configSpeciale));

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string sourceNonPrio = Path.Combine(Path.GetTempPath(), "ES_NonPrio_Src");
            string cibleNonPrio = Path.Combine(Path.GetTempPath(), "ES_NonPrio_Tgt");
            string sourcePrio = Path.Combine(Path.GetTempPath(), "ES_Prio_Src");
            string ciblePrio = Path.Combine(Path.GetTempPath(), "ES_Prio_Tgt");

            if (Directory.Exists(sourceNonPrio)) Directory.Delete(sourceNonPrio, true);
            if (Directory.Exists(sourcePrio)) Directory.Delete(sourcePrio, true);
            if (Directory.Exists(cibleNonPrio)) Directory.Delete(cibleNonPrio, true);
            if (Directory.Exists(ciblePrio)) Directory.Delete(ciblePrio, true);

            Directory.CreateDirectory(sourceNonPrio);
            Directory.CreateDirectory(sourcePrio);

            for (int i = 0; i < 50; i++)
            {
                File.WriteAllText(Path.Combine(sourceNonPrio, $"file_{i}.dat"), "Data");
                File.WriteAllText(Path.Combine(sourcePrio, $"doc_{i}.txt"), "Priority Data");
            }

            var bm = BackupManager.GetBM();
            foreach (var j in bm.GetAllJobs()) bm.DeleteJob(j.Id);

            bm.AddJob("Job_NonPrio", sourceNonPrio, cibleNonPrio, BackupType.Differential);
            bm.AddJob("Job_Prio", sourcePrio, ciblePrio, BackupType.Differential);

            bm.ExecuteAllJobs();

            int countNonPrio = Directory.Exists(cibleNonPrio) ? Directory.GetFiles(cibleNonPrio).Length : 0;
            int countPrio = Directory.Exists(ciblePrio) ? Directory.GetFiles(ciblePrio).Length : 0;

            Assert.AreEqual(50, countPrio);
            Assert.AreEqual(50, countNonPrio);

            try { Directory.Delete(sourceNonPrio, true); } catch { }
            try { Directory.Delete(sourcePrio, true); } catch { }
            try { Directory.Delete(cibleNonPrio, true); } catch { }
            try { Directory.Delete(ciblePrio, true); } catch { }
        }
        [TestMethod]
        public void TestLimiteTransfert_GrosFichiers()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");

            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                PriorityExtensions = new string[] { },
                MaxParallelTransferSize = 1,
                CryptoKey = "1234",
                CryptoSoftPath = ""
            };
            File.WriteAllText(configPath, JsonConvert.SerializeObject(configSpeciale));

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string srcGrosA = Path.Combine(Path.GetTempPath(), "ES_GrosA_Src");
            string tgtGrosA = Path.Combine(Path.GetTempPath(), "ES_GrosA_Tgt");
            string srcGrosB = Path.Combine(Path.GetTempPath(), "ES_GrosB_Src");
            string tgtGrosB = Path.Combine(Path.GetTempPath(), "ES_GrosB_Tgt");
            string srcPetit = Path.Combine(Path.GetTempPath(), "ES_Petit_Src");
            string tgtPetit = Path.Combine(Path.GetTempPath(), "ES_Petit_Tgt");

            if (Directory.Exists(srcGrosA)) Directory.Delete(srcGrosA, true);
            if (Directory.Exists(tgtGrosA)) Directory.Delete(tgtGrosA, true);
            if (Directory.Exists(srcGrosB)) Directory.Delete(srcGrosB, true);
            if (Directory.Exists(tgtGrosB)) Directory.Delete(tgtGrosB, true);
            if (Directory.Exists(srcPetit)) Directory.Delete(srcPetit, true);
            if (Directory.Exists(tgtPetit)) Directory.Delete(tgtPetit, true);

            Directory.CreateDirectory(srcGrosA);
            Directory.CreateDirectory(srcGrosB);
            Directory.CreateDirectory(srcPetit);

            string grosContenu = new string('A', 2000);
            for (int i = 0; i < 10; i++)
            {
                File.WriteAllText(Path.Combine(srcGrosA, $"grosA_{i}.bin"), grosContenu);
                File.WriteAllText(Path.Combine(srcGrosB, $"grosB_{i}.bin"), grosContenu);
                File.WriteAllText(Path.Combine(srcPetit, $"petit_{i}.txt"), "Petite data");
            }

            var bm = BackupManager.GetBM();
            foreach (var j in bm.GetAllJobs()) bm.DeleteJob(j.Id);

            bm.AddJob("Job_Gros_A", srcGrosA, tgtGrosA, BackupType.Complete);
            bm.AddJob("Job_Gros_B", srcGrosB, tgtGrosB, BackupType.Complete);
            bm.AddJob("Job_Petit", srcPetit, tgtPetit, BackupType.Complete);

            bm.ExecuteAllJobs();

            Assert.AreEqual(10, Directory.GetFiles(tgtGrosA).Length, "Fichiers Gros A manquants");
            Assert.AreEqual(10, Directory.GetFiles(tgtGrosB).Length, "Fichiers Gros B manquants");
            Assert.AreEqual(10, Directory.GetFiles(tgtPetit).Length, "Fichiers Petit manquants");

            try { Directory.Delete(srcGrosA, true); } catch { }
            try { Directory.Delete(tgtGrosA, true); } catch { }
            try { Directory.Delete(srcGrosB, true); } catch { }
            try { Directory.Delete(tgtGrosB, true); } catch { }
            try { Directory.Delete(srcPetit, true); } catch { }
            try { Directory.Delete(tgtPetit, true); } catch { }
        }
    }
}