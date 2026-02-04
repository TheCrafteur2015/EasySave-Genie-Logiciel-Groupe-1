using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Backup
{
	/// <summary>
	/// Backup job state
	/// </summary>
	public enum State
	{
		Inactive,
		Active,
		Completed,
		Error
	}
}
