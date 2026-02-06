namespace EasySave.View.Command
{
    public interface ICommand
    {
		int GetID();

		string GetI18nKey();
		
		void Execute();

	}
}
