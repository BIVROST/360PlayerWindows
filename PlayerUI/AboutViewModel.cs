using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class AboutViewModel : Screen
	{
		public AboutViewModel()
		{
			DisplayName = "About Bivrost Player";
		}

		public void OpenEULA()
		{
			DialogHelper.ShowDialog<EULAViewModel>();
		}

		public void ShowLibs()
		{
			DialogHelper.ShowDialog<EulaLibsViewModel>();
		}
	}
}
