namespace EasySave.Backup
{
	/// <summary>
	/// Factory for creating backup strategies (Factory Pattern)
	/// </summary>
	public static class BackupStrategyFactory
	{

		/// <summary>
		/// Creates a backup strategy instance based on the specified backup type.
		/// </summary>
		/// <param name="type">The type of backup for which to create a strategy. Must be a defined value of the BackupType enumeration.</param>
		/// <returns>An instance of a class that implements the IBackupStrategy interface corresponding to the specified backup type.</returns>
		/// <exception cref="NotImplementedException">Thrown if the specified backup type is Incremental, which is not currently supported.</exception>
		/// <exception cref="ArgumentException">Thrown if the specified backup type is not recognized.</exception>
		public static BackupStrategy CreateStrategy(BackupType type)
		{
			return type switch
			{
				BackupType.Complete     => new CompleteBackupStrategy(),
				BackupType.Differential => new DifferentialBackupStrategy(),
				BackupType.Incremental  => throw new NotImplementedException("This backup type is not yet implemented!"),
				_                       => throw new ArgumentException($"Unknown backup type: {type}")
			};
		}
	}
}