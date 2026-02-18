namespace EasySave.Backup
{
	/// <summary>
	/// Backup job state
	/// </summary>
	public enum State
	{
		Inactive,
		Active,
		Paused,
		Completed,
		Error
	}
}