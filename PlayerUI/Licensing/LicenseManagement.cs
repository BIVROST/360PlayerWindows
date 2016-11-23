using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Licensing
{
	public static class LicenseManagement
	{


		public static void OpenLicenseManagement(Action onLicenseCommit)
		{
			new LicenseManagementViewModel(true, onLicenseCommit);
		}


		public static void LicenseCheck(Action onLicenseCommit)
		{
			new LicenseManagementViewModel(false, onLicenseCommit);
		}
	}
}
