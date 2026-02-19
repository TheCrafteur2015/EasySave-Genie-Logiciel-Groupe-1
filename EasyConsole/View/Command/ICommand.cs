namespace EasyConsole.View.Command
{
	/// <summary>
	/// Interface defining the structure of a command in the application.
	/// Implements the Command pattern to encapsulate user actions.
	/// </summary>
	public interface ICommand
	{
		/// <summary>
		/// Gets the unique identifier of the command.
		/// This ID is used to map user input to the specific command in the menu.
		/// </summary>
		/// <returns>The unique integer identifier of the command.</returns>
		int GetID();

		/// <summary>
		/// Gets the localization key for the command's display name.
		/// </summary>
		/// <returns>The string key used to retrieve the localized description.</returns>
		string GetI18nKey();

		/// <summary>
		/// Executes the specific logic associated with the command.
		/// </summary>
		void Execute();

	}
}