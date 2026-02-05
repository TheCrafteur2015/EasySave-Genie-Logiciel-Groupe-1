namespace EasySave.Backup
{
	/// <summary>
	/// Factory for creating backup strategies (Factory Pattern)
	/// </summary>
	public class BackupStrategyFactory
	{

		private BackupStrategyFactory() {}

		public static IBackupStrategy CreateStrategy(BackupType type)
		{
			return type switch
			{
				BackupType.Complete     => new CompleteBackupStrategy(),
				BackupType.Differential => new DifferentialBackupStrategy(),
				BackupType.Incremental  => throw new NotImplementedException("This backup type is not yet implemented!"),
				_ => throw new ArgumentException($"Unknown backup type: {type}")
			};
		}
	}
}
