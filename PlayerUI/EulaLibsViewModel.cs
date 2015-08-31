using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class EulaLibsViewModel : Screen
	{
		public EulaLibsViewModel()
		{
			DisplayName = "3rd party libraries";
			LicenseText = Properties.Resources.EulaLibs;
		}

		public string LicenseText { get; set; }
	}
}
