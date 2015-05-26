using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class BivrostBootstrapper : BootstrapperBase
	{
		public BivrostBootstrapper()
		{
			Initialize();
		}

		protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
		{
			if(Logic.Instance.settings.EventMode)
				DisplayRootViewFor<EventShellViewModel>();
			else
				DisplayRootViewFor<ShellViewModel>();
		}
	}
}
