using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.ConfigUI
{
	public class StringConfigItemViewModel : ConfigItemBase<string>
	{
		public StringConfigItemViewModel(SettingsPropertyAttribute attr, Func<string> loadCallback, Action<string> saveCallback) : base(attr, loadCallback, saveCallback) { }

		public bool ReadOnly { get; set; } = false;
	}
}
