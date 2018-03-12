using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.ConfigUI
{
	class ButtonConfigItemViewModel : PropertyChangedBase
	{
		System.Action _callback;
		public ButtonConfigItemViewModel(SettingsPropertyAttribute attr, System.Action callback) {
			ActionText = attr.Caption;
			DisplayName = attr.DisplayName;
			_callback = callback;
		}

		public bool ReadOnly { get; set; } = false;

		public string ActionText { get; set; } = "";
		public string DisplayName { get; set; } = "";

		public void DoAction()
		{
			_callback();
		}
	}
}
