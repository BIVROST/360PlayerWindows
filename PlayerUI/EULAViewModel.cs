using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	public class EULAViewModel : Screen
	{
		public EULAViewModel()
		{
			DisplayName = "Bivrost 360Player license";
			LicenseText = Properties.Resources.EULA;
        }

		public string LicenseText { get; set; }
	}
}
