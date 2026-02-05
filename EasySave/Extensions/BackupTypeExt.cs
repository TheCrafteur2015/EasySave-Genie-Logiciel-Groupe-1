using EasySave.Backup;
using EasySave.View.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Extensions
{
    public static class BackupTypeExt
    {

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
