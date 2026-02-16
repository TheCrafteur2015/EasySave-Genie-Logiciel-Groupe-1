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

            DeleteDirectorySafe(_dossierSource);
            DeleteDirectorySafe(_dossierCible);

            Directory.CreateDirectory(_dossierSource);

            var bm = BackupManager.GetBM();
            var jobsIds = bm.GetAllJobs().Select(j => j.Id).ToList();
            foreach (var id in jobsIds) bm.DeleteJob(id);

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            KillCalculator();
        }

        private void DeleteDirectorySafe(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (FileInfo file in di.GetFiles()) { try { file.Delete(); } catch { } }
                        foreach (DirectoryInfo dir in di.GetDirectories()) { try { dir.Delete(true); } catch { } }
                    }
                    catch { }
                }
            }
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

            // CORRECTION : Utilisation de ExecuteJobAsync + Wait pour attendre la fin
            var task = bm.ExecuteJobAsync(job.Id);
            task.Wait();

            bool succes = job.State == State.Completed;

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

            bm.ExecuteJobAsync(id).Wait(); // Version Async + Wait
            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            DateTime datePremiereCopie = File.GetLastWriteTime(fichierCible);

            Thread.Sleep(1000);
            File.WriteAllText(fichierSource, "Version 2 - Modifié");

            bm.ExecuteJobAsync(id).Wait(); // Version Async + Wait
            DateTime dateDeuxiemeCopie = File.GetLastWriteTime(fichierCible);

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie);
            Assert.AreEqual("Version 2 - Modifié", File.ReadAllText(fichierCible));
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

            bm.ExecuteJobAsync(id).Wait();

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

            // CORRECTION MAJEURE ICI :
            // 1. On lance les tâches avec ExecuteAllJobsAsync
            // 2. On attend qu'elles soient TOUTES finies avec Task.WaitAll
            var tasks = bm.ExecuteAllJobsAsync();
            Task.WaitAll(tasks.ToArray());

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

            // CORRECTION ICI AUSSI :
            var tasks = bm.ExecuteAllJobsAsync();
            Task.WaitAll(tasks.ToArray());

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

        [TestMethod]
        public void TestInteraction_PauseResumeStop()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);

            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                PriorityExtensions = new string[] { },
                MaxParallelTransferSize = 100000,
                CryptoKey = "1234",
                CryptoSoftPath = ""
            };
            File.WriteAllText(Path.Combine(configDir, "config.json"), JsonConvert.SerializeObject(configSpeciale));

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string source = Path.Combine(Path.GetTempPath(), "ES_Interact_Src");
            string cible = Path.Combine(Path.GetTempPath(), "ES_Interact_Tgt");

            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(cible)) Directory.Delete(cible, true);
            Directory.CreateDirectory(source);

            for (int i = 0; i < 1000; i++)
            {
                File.WriteAllText(Path.Combine(source, $"file_{i}.txt"), "data");
            }

            var bm = BackupManager.GetBM();
            foreach (var j in bm.GetAllJobs()) bm.DeleteJob(j.Id);
            bm.AddJob("Job_Interact", source, cible, BackupType.Complete);
            var job = bm.GetAllJobs()[0];

            int fichiersRestants = 1000;

            Task task = bm.ExecuteJobAsync(job.Id, (state) =>
            {
                fichiersRestants = state.FilesRemaining;
            });

            Thread.Sleep(500);

            bm.PauseJob(job.Id);
            Thread.Sleep(1000);

            int fichiersRestantsPendantPause = fichiersRestants;

            Thread.Sleep(1000);

            Assert.AreEqual(fichiersRestantsPendantPause, fichiersRestants, "Le job ne doit pas progresser pendant la pause.");

            bm.ResumeJob(job.Id);
            Thread.Sleep(1000);

            Assert.AreNotEqual(fichiersRestantsPendantPause, fichiersRestants, "Le job doit reprendre sa progression après Resume.");
            Assert.IsTrue(fichiersRestants < fichiersRestantsPendantPause, "Le nombre de fichiers restants doit diminuer.");

            bm.StopJob(job.Id);

            task.Wait(2000);

            Assert.IsTrue(task.IsCompleted, "La tâche doit être terminée après un Stop.");
            Assert.AreEqual(State.Error, job.State, "L'état du job doit être 'Error' (ou Stopped) après une interruption utilisateur.");

            int fichiersCopies = Directory.Exists(cible) ? Directory.GetFiles(cible).Length : 0;
            Assert.IsTrue(fichiersCopies < 1000, $"Le job aurait dû être stoppé avant la fin (Copiés: {fichiersCopies}/1000).");

            try { Directory.Delete(source, true); } catch { }
            try { Directory.Delete(cible, true); } catch { }
        }
        [TestMethod]
        public void TestLogicielMetier_PauseEtReprise()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);

            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                BusinessSoftware = "CalculatorApp",
                PriorityExtensions = new string[] { },
                CryptoKey = "1234",
                CryptoSoftPath = ""
            };
            File.WriteAllText(Path.Combine(configDir, "config.json"), JsonConvert.SerializeObject(configSpeciale));

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            string source = Path.Combine(Path.GetTempPath(), "ES_Pause_Src");
            string cible = Path.Combine(Path.GetTempPath(), "ES_Pause_Tgt");
            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(cible)) Directory.Delete(cible, true);
            Directory.CreateDirectory(source);
            for (int i = 0; i < 5; i++) File.WriteAllText(Path.Combine(source, $"data{i}.txt"), "content");

            KillAllCalculators();

            try
            {
                Process.Start("calc.exe");
                Thread.Sleep(2000); 

                var bm = BackupManager.GetBM();
                bm.AddJob("TestPauseMetier", source, cible, BackupType.Complete);
                var job = bm.GetAllJobs().Last();

                Task task = bm.ExecuteJobAsync(job.Id);

                Thread.Sleep(3000);

                Assert.IsFalse(task.IsCompleted, "Le job doit être en pause (non fini) car la calculatrice est ouverte.");

                KillAllCalculators();

                task.Wait(5000);

                Assert.IsTrue(task.IsCompleted, "Le job aurait dû reprendre et finir après la fermeture de la calculatrice.");
                Assert.AreEqual(State.Completed, job.State);
                Assert.AreEqual(5, Directory.GetFiles(cible).Length);
            }
            finally
            {
                KillAllCalculators();
                try { Directory.Delete(source, true); } catch { }
                try { Directory.Delete(cible, true); } catch { }
            }
        }

        private void KillAllCalculators()
        {
            var names = new[] { "CalculatorApp", "calc", "Calculator", "win32calc" };
            foreach (var name in names)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); p.WaitForExit(100); } catch { }
                }
            }
        }
    }
}