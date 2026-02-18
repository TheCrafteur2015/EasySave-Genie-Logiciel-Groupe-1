namespace EasySave.Backup
{
	/// <summary>
	/// Represents the current state of a backup operation
	/// </summary>
	public class ProgressState
	{
		public string BackupName { get; set; }
		public DateTime Timestamp { get; set; }
		public State State { get; set; }
		public int TotalFiles { get; set; }
		public long TotalSize { get; set; }
		public int FilesRemaining { get; set; }
		public long SizeRemaining { get; set; }
		public string CurrentSourceFile { get; set; }
		public string CurrentTargetFile { get; set; }
		public double ProgressPercentage { get; set; }

		public string Message { get; set; }

		/// <summary>
		/// Initializes a new instance of the ProgressState class with default values.
		/// </summary>
		/// <remarks>The default constructor sets all string properties to empty strings, the timestamp to
		/// the current date and time, and the state to Inactive. This ensures the object starts in a consistent,
		/// inactive state before any progress tracking begins.</remarks>
		public ProgressState()
		{
			BackupName        = string.Empty;
			Timestamp         = DateTime.Now;
			State             = State.Inactive;
			CurrentSourceFile = string.Empty;
			CurrentTargetFile = string.Empty;
			Message           = string.Empty;
		}
	}
}