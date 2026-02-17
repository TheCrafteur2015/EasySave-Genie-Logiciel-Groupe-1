using System.Text.Json.Serialization;

namespace EasySave.Backup
{
	/// <summary>
	/// Represents a backup job configuration
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the BackupJob class with the specified job details and backup type.
	/// </remarks>
	/// <param name="Id">The unique identifier for the backup job.</param>
	/// <param name="Name">The name assigned to the backup job. Cannot be null or empty.</param>
	/// <param name="SourceDirectory">The path to the source directory to be backed up. Must be a valid directory path.</param>
	/// <param name="TargetDirectory">The path to the target directory where backups will be stored. Must be a valid directory path.</param>
	/// <param name="Type">The type of backup to perform for this job.</param>
	public class BackupJob(int Id, string Name, string SourceDirectory, string TargetDirectory, BackupType Type)
	{
		public int Id { get; set; } = Id;

		public string Name { get; set; } = Name;

		public string SourceDirectory { get; set; } = SourceDirectory;

		public string TargetDirectory { get; set; } = TargetDirectory;

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public BackupType Type { get; set; } = Type;

		[JsonIgnore]
		public IBackupStrategy Strategy { get; } = BackupStrategyFactory.CreateStrategy(Type);

		public DateTime LastExecution { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public State State { get; set; } = State.Inactive;

		[JsonIgnore]
		public ManualResetEventSlim PauseWaitHandle { get; private set; } = new ManualResetEventSlim(true);

		[JsonIgnore]
		public CancellationTokenSource Cts { get; private set; } = new CancellationTokenSource();

		/// <summary>
		/// Resets pause and cancellation controls for a fresh execution.
		/// </summary>
		public void ResetControls()
		{
			Cts?.Dispose();
			Cts = new CancellationTokenSource();
			PauseWaitHandle?.Dispose();
			PauseWaitHandle = new ManualResetEventSlim(true);
		}

		/// <summary>
		/// Executes the associated strategy and reports progress through the specified callback.
		/// </summary>
		/// <remarks>The method sets the state to active before execution and to completed after execution. The
		/// progress callback is invoked to provide updates during the execution process.</remarks>
		/// <param name="progressCallback">A callback method that receives progress updates as a <see cref="ProgressState"/> object. Cannot be null.</param>
		public void Execute(Action<ProgressState> progressCallback)
		{
			string BusinessSoftware = BackupManager.GetBM().ConfigManager.GetConfig("BusinessSoftware");
			State = State.Active;
			Strategy.Execute(this, BusinessSoftware, progressCallback);

			if (State != State.Error && State != State.Paused)
			{
				LastExecution = DateTime.Now;
				State = State.Completed;
			}
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

		public override bool Equals(object? obj)
		{
			if (obj == null)
				return false;
			if (obj == this)
				return true;
			if (obj is BackupJob job)
			{
				return job.Id == Id &&
					job.Name == Name &&
					job.SourceDirectory == SourceDirectory &&
					job.TargetDirectory == TargetDirectory &&
					job.Type == Type &&
					job.Strategy.GetType() == Strategy.GetType() &&
					job.State == State;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Name, SourceDirectory, TargetDirectory, Type, Strategy?.GetType(), State);
		}

		public override string ToString()
		{
			return $"Backup ID: {Id}, name: {Name}, Source: {SourceDirectory}, Destination: {TargetDirectory}, Type: {Type}, Strategy: {Strategy == null}, Last Execution: {LastExecution}, State: {State}";
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Name, SourceDirectory, TargetDirectory, Type, Strategy, LastExecution, State);
		}
	}
}