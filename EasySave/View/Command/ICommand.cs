using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.View.Command
{
    public interface ICommand
    {
		int GetID();

		string GetI18nKey();
		
		void Execute();

	}
}
