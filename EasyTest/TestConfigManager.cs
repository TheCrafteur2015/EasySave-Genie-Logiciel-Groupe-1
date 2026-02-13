using EasySave.Backup;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyTest
{
	[TestClass]
	public sealed class TestConfigManager
	{
		[TestMethod]
		public void TestBackupJobSerialization()
		{
			var job = new BackupJob(1, "Test", @"C:\xampp\htdocs\_source", @"C:\xampp\htdocs\_target", BackupType.Complete);

			string json = JsonSerializer.Serialize(job, new JsonSerializerOptions
			{
				WriteIndented = true,
				Converters = { new JsonStringEnumConverter() },
				IncludeFields = true
			});

			Assert.AreEqual(json,
"""
{
  "Id": 1,
  "Name": "Test",
  "SourceDirectory": "C:\\xampp\\htdocs\\_source",
  "TargetDirectory": "C:\\xampp\\htdocs\\_target",
  "Type": "Complete",
  "LastExecution": "0001-01-01T00:00:00",
  "State": "Inactive"
}
""");

			var job2 = JsonSerializer.Deserialize<BackupJob>(json);

			Assert.AreEqual(job, job2);
		}
	}
}