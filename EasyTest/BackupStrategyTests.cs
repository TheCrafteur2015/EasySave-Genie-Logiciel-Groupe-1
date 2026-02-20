using EasySave.Backup;
using EasySave.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace EasyTest
{
    /// <summary>
    /// Contains comprehensive integration and unit tests for backup strategies.
    /// Covers full/differential backups, CryptoSoft integration, and real-time interaction (Pause/Resume/Stop).
    /// </summary>
    [TestClass]
    public class BackupStrategyTests
    {
        private string _dossierSource = null!;
        private string _dossierCible = null!;

        private const string NomFichierTest = "fichier_test.txt";
        private const string NomFichierCrypto = "secret.txt";

        /// <summary>
        /// Sets up the test environment before each test method execution.
        /// Cleans up processes, temporary directories, and resets the BackupManager singleton.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            KillProcesses();

            typeof(BackupManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);

            _dossierSource = Path.Combine(Path.GetTempPath(), "EasySave_Source_Test");
            _dossierCible = Path.Combine(Path.GetTempPath(), "EasySave_Cible_Test");

            DeleteDirectorySafe(_dossierSource);
            DeleteDirectorySafe(_dossierCible);

            Directory.CreateDirectory(_dossierSource);

            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            DeleteDirectorySafe(configDir);

            var bm = BackupManager.GetBM();
            var jobsIds = bm.GetAllJobs().Select(j => j.Id).ToList();
            foreach (var id in jobsIds) bm.DeleteJob(id);
        }

        /// <summary>
        /// Cleans up the test environment after each test method execution.
        /// Ensures all monitored processes are closed.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            KillProcesses();
        }

        /// <summary>
        /// Robustly locates the solution root directory by searching for the .sln file.
        /// </summary>
        /// <returns>The full path to the solution directory.</returns>
        private string GetSolutionDirectory()
        {
            var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            // Traverse up the directory tree until a .sln file is found
            while (currentDirectory != null && !currentDirectory.GetFiles("*.sln").Any())
            {
                currentDirectory = currentDirectory.Parent;
            }
            return currentDirectory?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Tests the integration with the external CryptoSoft tool.
        /// Verifies that files with specified extensions are encrypted during the backup process.
        /// </summary>
        [TestMethod]
        public void TestCryptoSoft_Integration()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);
            string configPath = Path.Combine(configDir, "config.json");

            string solutionDir = GetSolutionDirectory();

            // Locate the CryptoSoft executable, excluding build artifacts (obj folders)
            var foundFiles = Directory.GetFiles(solutionDir, "CryptoSoft.exe", SearchOption.AllDirectories)
                                      .Where(p => !p.Contains("obj"))
                                      .Select(p => new FileInfo(p))
                                      .OrderByDescending(f => f.LastWriteTime)
                                      .ToList();

            if (foundFiles.Count == 0)
            {
                Assert.Inconclusive($"🚨 CryptoSoft.exe not found in: {solutionDir}\n" +
                                    $"👉 ACTION: Please Build the 'CryptoSoft' project in the Solution Explorer.");
            }

            string cryptoPath = foundFiles.First().FullName;
            Console.WriteLine($"✅ CryptoSoft found at: {cryptoPath}");

            // Create a specific configuration for the encryption test
            var configSpeciale = new
            {
                Version = "1.0.0",
                MaxBackupJobs = 5,
                PriorityExtensions = new[] { ".txt" },
                CryptoKey = "MaCleSecrete",
                CryptoSoftPath = cryptoPath
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
            Assert.IsTrue(File.Exists(cheminFichierCible), "The target file must exist.");

            string contenuCible = File.ReadAllText(cheminFichierCible);
            Assert.AreNotEqual(contenuClair, contenuCible, "The target file should be encrypted (content differs from source).");
        }

        /// <summary>
        /// Tests the full backup strategy to ensure files are correctly copied.
        /// </summary>
        [TestMethod]
        public void TestSauvegardeComplete_CopieFichiers()
        {
            string cheminFichierSource = Path.Combine(_dossierSource, NomFichierTest);
            File.WriteAllText(cheminFichierSource, "Contenu de test");

            var bm = BackupManager.GetBM();
            bm.AddJob("TestComplet", _dossierSource, _dossierCible, BackupType.Complete);
            var job = bm.GetAllJobs()[0];

            var task = bm.ExecuteJobAsync(job.Id);
            task.Wait();

            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            Assert.IsTrue(File.Exists(fichierCible));
            Assert.AreEqual("Contenu de test", File.ReadAllText(fichierCible));
        }

        /// <summary>
        /// Tests the differential backup strategy.
        /// Verifies that modified files are updated in the target directory while unchanged files are ignored.
        /// </summary>
        [TestMethod]
        public void TestSauvegardeDifferentielle_FichierModifie()
        {
            string fichierSource = Path.Combine(_dossierSource, NomFichierTest);
            File.WriteAllText(fichierSource, "Version 1");

            var bm = BackupManager.GetBM();
            bm.AddJob("TestDiff", _dossierSource, _dossierCible, BackupType.Differential);
            int id = bm.GetAllJobs()[0].Id;

            bm.ExecuteJobAsync(id).Wait();
            string fichierCible = Path.Combine(_dossierCible, NomFichierTest);
            DateTime datePremiereCopie = File.GetLastWriteTime(fichierCible);

            Thread.Sleep(1100); // Ensure timestamp difference
            File.WriteAllText(fichierSource, "Version 2 - Modifié");

            bm.ExecuteJobAsync(id).Wait();
            DateTime dateDeuxiemeCopie = File.GetLastWriteTime(fichierCible);

            Assert.AreNotEqual(datePremiereCopie, dateDeuxiemeCopie);
            Assert.AreEqual("Version 2 - Modifié", File.ReadAllText(fichierCible));
        }

        /// <summary>
        /// Tests the parallel transfer limits for large files.
        /// Verifies that the system correctly manages concurrent tasks when specific file size thresholds are met.
        /// </summary>
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

            var tasks = bm.ExecuteAllJobsAsync();
            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(10, Directory.GetFiles(tgtGrosA).Length);
            Assert.AreEqual(10, Directory.GetFiles(tgtGrosB).Length);
            Assert.AreEqual(10, Directory.GetFiles(tgtPetit).Length);

            try { Directory.Delete(srcGrosA, true); } catch { }
            try { Directory.Delete(tgtGrosA, true); } catch { }
            try { Directory.Delete(srcGrosB, true); } catch { }
            try { Directory.Delete(tgtGrosB, true); } catch { }
            try { Directory.Delete(srcPetit, true); } catch { }
            try { Directory.Delete(tgtPetit, true); } catch { }
        }

        /// <summary>
        /// Tests manual user interactions: Pause, Resume, and Stop.
        /// Verifies that the backup thread respects control signals and stops/resumes progress accordingly.
        /// </summary>
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

            Assert.AreEqual(fichiersRestantsPendantPause, fichiersRestants, "Job should not progress during pause.");

            bm.ResumeJob(job.Id);
            Thread.Sleep(1000);

            Assert.AreNotEqual(fichiersRestantsPendantPause, fichiersRestants, "Job should resume progress after Resume signal.");
            Assert.IsTrue(fichiersRestants < fichiersRestantsPendantPause, "Remaining file count should decrease.");

            bm.StopJob(job.Id);

            task.Wait(2000);

            Assert.IsTrue(task.IsCompleted, "Task should complete (terminate) after Stop signal.");
            Assert.AreEqual(State.Error, job.State, "Job state should be 'Error' or stopped after user interruption.");

            int fichiersCopies = Directory.Exists(cible) ? Directory.GetFiles(cible).Length : 0;
            Assert.IsTrue(fichiersCopies < 1000, $"Job should have stopped before completion (Copied: {fichiersCopies}/1000).");

            try { Directory.Delete(source, true); } catch { }
            try { Directory.Delete(cible, true); } catch { }
        }

        /// <summary>
        /// Tests automatic pause and resume based on business software detection.
        /// Verifies that the backup thread pauses when a specific process (e.g., notepad) is detected.
        /// </summary>
        [TestMethod]
        public void TestLogicielMetier_PauseEtReprise()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            string configDir = Path.Combine(appData, "Config");
            Directory.CreateDirectory(configDir);

            // Using notepad for reliability in automated tests
            var configSpeciale = new
            {
                Version = "1.1.0",
                MaxBackupJobs = 5,
                BusinessSoftware = "notepad",
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

            KillProcesses();

            try
            {
                Process.Start("notepad.exe");
                Thread.Sleep(2000);

                var bm = BackupManager.GetBM();
                bm.AddJob("TestPauseMetier", source, cible, BackupType.Complete);
                var job = bm.GetAllJobs().Last();

                Task task = bm.ExecuteJobAsync(job.Id);

                Thread.Sleep(3000);

                Assert.IsFalse(task.IsCompleted, "Job should be paused (unfinished) because Notepad is open.");

                KillProcesses();

                task.Wait(8000); // Allow time for detection loop to resume

                Assert.IsTrue(task.IsCompleted, "Job should have resumed and finished after Notepad closure.");
                Assert.AreEqual(State.Completed, job.State);
                Assert.AreEqual(5, Directory.GetFiles(cible).Length);
            }
            finally
            {
                KillProcesses();
                try { Directory.Delete(source, true); } catch { }
                try { Directory.Delete(cible, true); } catch { }
            }
        }

        /// <summary>
        /// Forcefully terminates known test-related processes to ensure a clean state.
        /// </summary>
        private void KillProcesses()
        {
            var names = new[] { "CalculatorApp", "calc", "Calculator", "win32calc", "notepad" };
            foreach (var name in names)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(); p.WaitForExit(100); } catch { }
                }
            }
        }

        /// <summary>
        /// Robustly deletes a directory and its contents, handling potential file locks.
        /// </summary>
        /// <param name="path">The directory path to delete.</param>
        private void DeleteDirectorySafe(string path)
        {
            if (Directory.Exists(path))
            {
                try { Directory.Delete(path, true); }
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

        /// <summary>
        /// Utility method to kill calculator processes, kept for backward compatibility.
        /// </summary>
        private void KillCalculator() => KillProcesses();
    }
}