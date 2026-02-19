using EasySave.Backup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace EasyTest
{
	/// <summary>
	/// Contains unit tests to verify the stability and correctness of backup jobs 
	/// executing in parallel.
	/// </summary>
	[TestClass]
	public class BackupParallelTests
	{
		private string _source1 = null!;
		private string _target1 = null!;
		private string _source2 = null!;
		private string _target2 = null!;

		/// <summary>
		/// Initializes the test environment before each test execution.
		/// Resets the BackupManager and prepares temporary directories.
		/// </summary>
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

		/// <summary>
		/// Cleans up the temporary test environment after each test execution.
		/// </summary>
		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_source1)) Directory.Delete(_source1, true);
			if (Directory.Exists(_target1)) Directory.Delete(_target1, true);
			if (Directory.Exists(_source2)) Directory.Delete(_source2, true);
			if (Directory.Exists(_target2)) Directory.Delete(_target2, true);
		}

		/// <summary>
		/// Tests the stability of the system when running multiple backup jobs simultaneously.
		/// Verifies that all files are correctly copied and job states are updated properly.
		/// </summary>
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

			var tasks = bm.ExecuteAllJobsAsync();
			Task.WaitAll(tasks.ToArray());

			int count1 = Directory.GetFiles(_target1).Length;
			int count2 = Directory.GetFiles(_target2).Length;

			Assert.AreEqual(50, count1, "Job A should have copied 50 files.");
			Assert.AreEqual(50, count2, "Job B should have copied 50 files.");

			var jobs = bm.GetAllJobs();
			Assert.AreEqual(State.Completed, jobs[0].State, "Job A state should be Completed.");
			Assert.AreEqual(State.Completed, jobs[1].State, "Job B state should be Completed.");
		}
	}
}