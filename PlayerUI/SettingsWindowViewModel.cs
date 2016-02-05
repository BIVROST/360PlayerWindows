using Caliburn.Micro;
using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class SettingsWindowViewModel : Screen
	{
		public SettingsWindowViewModel()
		{
			DisplayName = "Ustawienia";
		}

		public bool EventMode { get { return Logic.Instance.settings.EventMode; } set { Logic.Instance.settings.EventMode = value; } }
		public string EventModeSingleFile { get { return Logic.Instance.settings.EventModeSingleFile; } set { Logic.Instance.settings.EventModeSingleFile = value; } }
		public bool EventModeAutoPlay { get { return Logic.Instance.settings.EventModeAutoPlay; } set { Logic.Instance.settings.EventModeAutoPlay = value; } }
		public bool EventModePauseAtStartup { get { return Logic.Instance.settings.EventModePauseAtStartup; } set { Logic.Instance.settings.EventModePauseAtStartup = value; } }
		public string EventModeBackgroundColor { get { return Logic.Instance.settings.EventModeBackgroundColor; } set { Logic.Instance.settings.EventModeBackgroundColor = value; } }

		public bool EventModeLoop { get { return Logic.Instance.settings.EventModeLoop; } set { Logic.Instance.settings.EventModeLoop = value; } }

		public bool AutoPlay { get { return Logic.Instance.settings.AutoPlay; } set { Logic.Instance.settings.AutoPlay = value; } }
		public bool AutoLoad { get { return Logic.Instance.settings.AutoLoad; } set { Logic.Instance.settings.AutoLoad = value; } }
		public bool StartInFullScreen { get { return Logic.Instance.settings.StartInFullScreen; } set { Logic.Instance.settings.StartInFullScreen = value; } }
		public string AutoPlayFile { get { return Logic.Instance.settings.AutoPlayFile; } set { Logic.Instance.settings.AutoPlayFile = value; } }

		protected override void OnViewReady(object view)
		{
			base.OnViewReady(view);
			IconHelper.RemoveIcon(view as System.Windows.Window);
		}

		public void Reboot()
		{
			Logic.Instance.ReloadPlayer();
		}

		public void Save()
		{
			Logic.Instance.settings.Save();
		}

		public void Close()
		{
			TryClose();
		}
	}
}
