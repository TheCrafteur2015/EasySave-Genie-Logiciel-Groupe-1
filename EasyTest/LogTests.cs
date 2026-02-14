using EasyLog.Data;
using EasyLog.Logging;

namespace EasyTest
{
    [TestClass]
    public class LogTests
    {
        private string _dossierLogs = null!;

        [TestInitialize]
        public void Setup()
        {
            _dossierLogs = Path.Combine(Path.GetTempPath(), "EasySave_Logs_Test");
            if (Directory.Exists(_dossierLogs)) Directory.Delete(_dossierLogs, true);
            Directory.CreateDirectory(_dossierLogs);
        }

        [TestMethod]
        public void TestJsonLogger_CreationFichier()
        {
            var logger = LoggerFactory.CreateLogger("json", _dossierLogs);
            var entree = new LogEntry
            {
                Name = "SauvegardeTestJSON",
                SourceFile = @"C:\Source\doc.txt",
                TargetFile = @"C:\Cible\doc.txt",
                FileSize = 1024,
                ElapsedTime = 50,
                EncryptionTime = 15 
            };

            logger.Log(entree);

            string fichierLog = Path.Combine(_dossierLogs, $"{DateTime.Now:yyyy-MM-dd}.json");
            Assert.IsTrue(File.Exists(fichierLog), "Le fichier de log JSON aurait dû être créé.");

            string contenu = File.ReadAllText(fichierLog);
            Assert.IsTrue(contenu.Contains("SauvegardeTestJSON"), "Le log doit contenir le nom de la sauvegarde.");
            Assert.IsTrue(contenu.Contains("\"EncryptionTime\": 15"), "Le log doit contenir le temps de cryptage.");
        }

        [TestMethod]
        public void TestXmlLogger_CreationFichier()
        {
            var logger = LoggerFactory.CreateLogger("xml", _dossierLogs);
            var entree = new LogEntry
            {
                Name = "SauvegardeTestXML",
                SourceFile = "Source",
                TargetFile = "Target",
                FileSize = 500,
                ElapsedTime = 20
            };

            logger.Log(entree);

            string fichierLog = Path.Combine(_dossierLogs, $"{DateTime.Now:yyyy-MM-dd}.xml");
            Assert.IsTrue(File.Exists(fichierLog), "Le fichier de log XML aurait dû être créé.");

            string contenu = File.ReadAllText(fichierLog);
            Assert.IsTrue(contenu.Contains("<Name>SauvegardeTestXML</Name>"), "Le log doit contenir les balises XML correctes.");
        }
    }
}