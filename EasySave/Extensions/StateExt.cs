using EasySave.Backup;
using EasySave.View.Localization;

namespace EasySave.Extensions
{
    public static class StateExt
    {

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
