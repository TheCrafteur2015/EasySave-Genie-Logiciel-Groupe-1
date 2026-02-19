using EasySave.Backup;

[assembly: DoNotParallelize]

namespace EasyTest
{
	/// <summary>
	/// Unit tests for the <see cref="BackupManager"/> class.
	/// Ensures core functionalities like job management and singleton behavior work as expected.
	/// </summary>
	[TestClass]
	public class BackupManagerTests
	{
		/// <summary>
		/// Initializes the test environment before each test method execution.
		/// Clears all existing backup jobs to ensure a clean state for every test.
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
		}

		/// <summary>
		/// Tests the successful addition of a backup job.
		/// </summary>
		/// <remarks>
		/// Verifies that the manager returns true upon creation and that the job is correctly added to the list.
		/// </remarks>
		[TestMethod]
		public void TestAjoutTravail_Succes()
		{
			var bm = BackupManager.GetBM();

			bool resultat = bm.AddJob("TravailTest", @"C:\Source", @"C:\Cible", BackupType.Complete);

			Assert.IsTrue(resultat, "The job should have been added successfully.");
			Assert.AreEqual(1, bm.GetAllJobs().Count, "There should be exactly 1 job in the list.");
			Assert.AreEqual("TravailTest", bm.GetAllJobs()[0].Name);
		}

		/// <summary>
		/// Tests that adding a job with invalid or empty parameters fails.
		/// </summary>
		/// <remarks>
		/// Ensures that the application does not create corrupted jobs with empty names or paths.
		/// </remarks>
		[TestMethod]
		public void TestAjoutTravail_Echec_CheminsInvalides()
		{
			var bm = BackupManager.GetBM();

			bool resultat = bm.AddJob("", "", "", BackupType.Complete);

			Assert.IsFalse(resultat, "The job should not be created with empty paths.");
			Assert.AreEqual(0, bm.GetAllJobs().Count);
		}

		/// <summary>
		/// Tests the successful deletion of an existing backup job.
		/// </summary>
		/// <remarks>
		/// Adds a job, retrieves its ID, deletes it, and verifies the list is empty.
		/// </remarks>
		[TestMethod]
		public void TestSuppressionTravail_Succes()
		{
			var bm = BackupManager.GetBM();
			bm.AddJob("TravailASupprimer", @"C:\Source", @"C:\Cible", BackupType.Complete);
			int id = bm.GetAllJobs()[0].Id;

			bool resultat = bm.DeleteJob(id);

			Assert.IsTrue(resultat, "Deletion should return true.");
			Assert.AreEqual(0, bm.GetAllJobs().Count, "The job list should be empty.");
		}

		/// <summary>
		/// Verifies the Singleton pattern implementation of the BackupManager.
		/// </summary>
		/// <remarks>
		/// Ensures that multiple calls to GetBM() return the exact same object instance.
		/// </remarks>
		[TestMethod]
		public void TestSingleton_InstanceUnique()
		{
			var instance1 = BackupManager.GetBM();
			var instance2 = BackupManager.GetBM();

			Assert.AreSame(instance1, instance2, "Both variables must point to the same instance (Singleton).");
		}
	}
}