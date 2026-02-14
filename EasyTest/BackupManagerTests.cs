using EasySave.Backup;

[assembly: DoNotParallelize]

namespace EasyTest
{
    [TestClass]
    public class BackupManagerTests
    {
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

        [TestMethod]
        public void TestAjoutTravail_Succes()
        {
            var bm = BackupManager.GetBM();

            bool resultat = bm.AddJob("TravailTest", @"C:\Source", @"C:\Cible", BackupType.Complete);

            Assert.IsTrue(resultat, "Le travail aurait dû être ajouté avec succès.");
            Assert.AreEqual(1, bm.GetAllJobs().Count, "Il devrait y avoir exactement 1 travail dans la liste.");
            Assert.AreEqual("TravailTest", bm.GetAllJobs()[0].Name);
        }

        [TestMethod]
        public void TestAjoutTravail_Echec_CheminsInvalides()
        {
            var bm = BackupManager.GetBM();

            bool resultat = bm.AddJob("", "", "", BackupType.Complete);

            Assert.IsFalse(resultat, "Le travail ne doit pas être créé avec des chemins vides.");
            Assert.AreEqual(0, bm.GetAllJobs().Count);
        }

        [TestMethod]
        public void TestSuppressionTravail_Succes()
        {
            var bm = BackupManager.GetBM();
            bm.AddJob("TravailASupprimer", @"C:\Source", @"C:\Cible", BackupType.Complete);
            int id = bm.GetAllJobs()[0].Id;

            bool resultat = bm.DeleteJob(id);

            Assert.IsTrue(resultat, "La suppression aurait dû retourner vrai.");
            Assert.AreEqual(0, bm.GetAllJobs().Count, "La liste des travaux devrait être vide.");
        }

        [TestMethod]
        public void TestSingleton_InstanceUnique()
        {
            var instance1 = BackupManager.GetBM();
            var instance2 = BackupManager.GetBM();

            Assert.AreSame(instance1, instance2, "Les deux variables doivent pointer vers la même instance (Singleton).");
        }
    }
}