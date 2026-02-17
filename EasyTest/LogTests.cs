using EasyLog.Data;
using EasyLog.Logging;

namespace EasyTest
{
    /// <summary>
    /// Unit tests for the logging system.
    /// Validates the creation and content of log files in both JSON and XML formats.
    /// </summary>
    [TestClass]
    public class LogTests
    {
        /// <summary>
        /// Path to the temporary directory used for log testing.
        /// </summary>
        private string _dossierLogs = null!;

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        /// <remarks>
        /// Creates a clean temporary directory for logs to ensure test isolation.
        /// </remarks>
        [TestInitialize]
        public void Setup()
        {
            _dossierLogs = Path.Combine(Path.GetTempPath(), "EasySave_Logs_Test");
            if (Directory.Exists(_dossierLogs)) Directory.Delete(_dossierLogs, true);
            Directory.CreateDirectory(_dossierLogs);
        }

        /// <summary>
        /// Verifies that the JSON logger correctly creates a file and writes valid data.
        /// </summary>
        /// <remarks>
        /// Checks for the existence of the file named with the current date and confirms 
        /// that specific fields like backup name and encryption time are present in the output.
        /// </remarks>
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
            Assert.IsTrue(File.Exists(fichierLog), "The JSON log file should have been created.");

            string contenu = File.ReadAllText(fichierLog);
            Assert.IsTrue(contenu.Contains("SauvegardeTestJSON"), "The log must contain the backup name.");
            Assert.IsTrue(contenu.Contains("\"EncryptionTime\": 15"), "The log must contain the encryption time.");
        }

        /// <summary>
        /// Verifies that the XML logger correctly creates a file and writes valid formatted data.
        /// </summary>
        /// <remarks>
        /// Checks for the existence of the XML file and verifies that the data is wrapped 
        /// in the correct XML tags.
        /// </remarks>
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
            Assert.IsTrue(File.Exists(fichierLog), "The XML log file should have been created.");

            string contenu = File.ReadAllText(fichierLog);
            Assert.IsTrue(contenu.Contains("<Name>SauvegardeTestXML</Name>"), "The log must contain the correct XML tags.");
        }
    }
}