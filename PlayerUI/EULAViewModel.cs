using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class EULAViewModel : Screen
	{
		public EULAViewModel()
		{
			DisplayName = "End-user license agreement";
			LicenseText = Properties.Resources.EULA;
        }

		public string LicenseText { get; set; }
	}
}
