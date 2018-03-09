using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.ConfigUI
{
	public class BoolConfigItemViewModel : ConfigItemBase<bool>
	{
		public BoolConfigItemViewModel(SettingsPropertyAttribute attr, Func<bool> loadCallback, Action<bool> saveCallback) : base(attr, loadCallback, saveCallback) { }

		public bool ReadOnly { get; set; } = false;
		public bool Enabled { get { return !this.ReadOnly; } }

		public void ToggleValue()
		{
			Value = !Value;
			NotifyOfPropertyChange(null);
		}
	}
}
