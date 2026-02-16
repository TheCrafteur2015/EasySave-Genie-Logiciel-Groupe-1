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

        public BackupType Type { get; }

        public IBackupStrategy Strategy { get; }

		public DateTime LastExecution { get; private set; }

		public State State { get; private set; }

		/// <summary>
		/// Initializes a new instance of the BackupJob class with the specified job details and backup type.
		/// </summary>
		/// <param name="id">The unique identifier for the backup job.</param>
		/// <param name="name">The name assigned to the backup job. Cannot be null or empty.</param>
		/// <param name="sourceDir">The path to the source directory to be backed up. Must be a valid directory path.</param>
		/// <param name="targetDir">The path to the target directory where backups will be stored. Must be a valid directory path.</param>
		/// <param name="type">The type of backup to perform for this job.</param>
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

		/// <summary>
		/// Executes the associated strategy and reports progress through the specified callback.
		/// </summary>
		/// <remarks>The method sets the state to active before execution and to completed after execution. The
		/// progress callback is invoked to provide updates during the execution process.</remarks>
		/// <param name="progressCallback">A callback method that receives progress updates as a <see cref="ProgressState"/> object. Cannot be null.</param>
		public void Execute(Action<ProgressState> progressCallback)
		{
			State = State.Active;
			Strategy.Execute(this, progressCallback);
			LastExecution = DateTime.Now;
			State = State.Completed;
		}

		/// <summary>
		/// Transitions the current state to indicate an error has occurred.	
		/// </summary>
		/// <remarks>Call this method to set the object's state to an error condition. This may affect subsequent
		/// operations that depend on the current state.</remarks>
		public void Error()
		{
			State = State.Error;
		}


	}

	
}
