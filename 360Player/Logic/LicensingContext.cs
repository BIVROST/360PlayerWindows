#if FEATURE_LICENSE_NINJA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bivrost.Licensing;

namespace Bivrost.Bivrost360Player
{
	public class LicensingContext : LicensingConnector.IContext
	{
		public string LicenseCode
		{
			get { return Logic.Instance.settings.LicenseCode; }
			set
			{
				Logic.Instance.settings.LicenseCode = value;
				Logic.Instance.settings.Save();
			}
		}


		public bool RequireLicense { get { return Features.RequireLicense; } }


		public string InstallId { get { return Logic.Instance.settings.InstallId.ToString(); } }


		public string ProductCode { get { return Logic.productCode; } }


		public void QuitApplication()
		{
			ShellViewModel.Instance.Quit();
		}


		public void LicenseUpdated(LicenseNinja.License license)
		{
			if (license == null)
				Features.SetBasicFeatures();
			else
				Features.SetGrants(license.GrantAsDictionary);
			Features.TriggerListUpdated();
		}


		public void LicenseVerified()
		{
			Logic.Notify("License verified");
		}

	}
}
#endif