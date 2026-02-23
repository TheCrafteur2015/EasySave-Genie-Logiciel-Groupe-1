using EasySave.Backup;
using EasySave.View.Localization;

namespace EasySave.Extensions
{
	/// <summary>
	/// Provides extension methods for the BackupType enumeration to facilitate localization and display.
	/// </summary>
	public static class BackupTypeExt
	{

		/// <summary>
		/// Retrieves the localized string representation of the specified backup type.
		/// </summary>
		/// <param name="type">The backup type to translate. Can be Complete, Differential, or Incremental.</param>
		/// <returns>A localized string corresponding to the backup type found in the I18n resources. Returns "not_translated" for unsupported types.</returns>
		public static string GetTranslation(this BackupType type)
		{
			return I18n.Instance.GetString(type switch
			{
				BackupType.Complete     => "type_complete",
				BackupType.Differential => "type_differential",
				BackupType.Incremental  => "not_translated",
				_                       => "not_translated",
			});
		}
	}
}