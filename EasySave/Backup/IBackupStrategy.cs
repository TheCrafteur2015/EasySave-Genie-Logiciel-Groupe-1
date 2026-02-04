using EasySave.Models;

namespace EasySave.Backup
{
	/// <summary>
	/// Strategy interface for backup implementations (Strategy Pattern)
	/// </summary>
	public interface IBackupStrategy
	{
		/// <summary>
		/// Executes the backup strategy
		/// </summary>
		void Execute(BackupJob job, Action<ProgressState> progressCallback);
	}
}
