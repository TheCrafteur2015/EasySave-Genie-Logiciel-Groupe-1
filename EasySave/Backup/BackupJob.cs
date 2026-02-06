namespace EasySave.Backup
{
	/// <summary>
	/// Represents a backup job configuration
	/// </summary>
	public class BackupJob
	{
		public int Id { get; }

		public string Name { get; }

		public string SourceDirectory { get; set; }

		public string TargetDirectory { get; set; }

		public readonly BackupType Type;

		public IBackupStrategy Strategy { get; }

		public DateTime LastExecution { get; private set; }

		public State State { get; private set; }

		public BackupJob(int id, string name, string sourceDir, string targetDir, BackupType type)
		{
			

			Id              = id;
			Name            = name;
			SourceDirectory = sourceDir;
			TargetDirectory = targetDir;
			State           = State.Inactive;
			Type            = type;
			Strategy        = BackupStrategyFactory.CreateStrategy(type);
		}

		public void Execute(Action<ProgressState> progressCallback)
		{
			State = State.Active;
			Strategy.Execute(this, progressCallback);
			LastExecution = DateTime.Now;
			State = State.Completed;
		}

		public void Error()
		{
			State = State.Error;
		}


	}

	
}
