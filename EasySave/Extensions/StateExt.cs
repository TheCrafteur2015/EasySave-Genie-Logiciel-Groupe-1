using EasySave.Backup;
using EasySave.View.Localization;

namespace EasySave.Extensions
{
	/// <summary>
	/// Provides extension methods for the State enumeration to facilitate localization and display.
	/// </summary>
	public static class StateExt
	{

		/// <summary>
		/// Retrieves the localized string representation of the specified backup state.
		/// </summary>
		/// <param name="state">The backup state to translate. Can be Inactive, Active, Completed, or Error.</param>
		/// <returns>A localized string corresponding to the state found in the I18n resources. Returns the translation for "state_inactive" if the state is not recognized.</returns>
		public static string GetTranslation(this State state)
		{
			return I18n.Instance.GetString(state switch
			{
				State.Inactive  => "state_inactive",
				State.Active    => "state_active",
				State.Completed => "state_completed",
				State.Error     => "state_error",
				_               => "state_inactive"
			});
		}

	}
}