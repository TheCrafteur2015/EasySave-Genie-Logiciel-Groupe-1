using EasySave.Backup;
using EasySave.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyTest
{
    [TestClass]
    public class BackupParallelTests
    {
        private string _source1 = null!;
        private string _target1 = null!;
        private string _source2 = null!;
        private string _target2 = null!;

        [TestInitialize]
        public void Setup()
        {
            var bm = BackupManager.GetBM();
            var jobs = bm.GetAllJobs();
            foreach (var job in jobs)
            {
                bm.DeleteJob(job.Id);
            }

            string temp = Path.GetTempPath();
            _source1 = Path.Combine(temp, "ES_Parallel_Src1");
            _target1 = Path.Combine(temp, "ES_Parallel_Tgt1");
            _source2 = Path.Combine(temp, "ES_Parallel_Src2");
            _target2 = Path.Combine(temp, "ES_Parallel_Tgt2");

            if (Directory.Exists(_source1)) Directory.Delete(_source1, true);
            if (Directory.Exists(_target1)) Directory.Delete(_target1, true);
            if (Directory.Exists(_source2)) Directory.Delete(_source2, true);
            if (Directory.Exists(_target2)) Directory.Delete(_target2, true);

            Directory.CreateDirectory(_source1);
            Directory.CreateDirectory(_source2);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_source1)) Directory.Delete(_source1, true);
            if (Directory.Exists(_target1)) Directory.Delete(_target1, true);
            if (Directory.Exists(_source2)) Directory.Delete(_source2, true);
            if (Directory.Exists(_target2)) Directory.Delete(_target2, true);
        }

        [TestMethod]
        public void TestExecutionParallele_Stabilite()
        {
            for (int i = 0; i < 50; i++)
            {
                File.WriteAllText(Path.Combine(_source1, $"file_A_{i}.txt"), "Contenu Job A");
                File.WriteAllText(Path.Combine(_source2, $"file_B_{i}.txt"), "Contenu Job B");
            }

            var bm = BackupManager.GetBM();

            bm.AddJob("JobParallele_A", _source1, _target1, BackupType.Complete);
            bm.AddJob("JobParallele_B", _source2, _target2, BackupType.Complete);

            bm.ExecuteAllJobs();

            int count1 = Directory.GetFiles(_target1).Length;
            int count2 = Directory.GetFiles(_target2).Length;

            Assert.AreEqual(50, count1, "Le Job A aurait dû copier 50 fichiers.");
            Assert.AreEqual(50, count2, "Le Job B aurait dû copier 50 fichiers.");

            var jobs = bm.GetAllJobs();
            Assert.AreEqual(State.Completed, jobs[0].State, "Le Job A doit être terminé.");
            Assert.AreEqual(State.Completed, jobs[1].State, "Le Job B doit être terminé.");
        }
    }
}