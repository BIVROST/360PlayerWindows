using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class LicenseManagementViewModel : Screen
	{
#if DEBUG

		public bool IsValid { get; private set; } = false;
		public string LicenseCode { get; set; }
		public bool CanValidate { get; set; } = true;

		public LicenseManagementViewModel()
		{
			DisplayName = "Enter valid license key";
			LicenseCode = Logic.Instance.settings.LicenseCode;
			Task.Factory.StartNew(async () =>
			{
				try
				{
					long seconds = await Bivrost.LicenseNinja.Verify(Logic.Instance.settings.ProductCode, LicenseCode, Logic.Instance.settings.InstallId.ToString());
					IsValid = true;
				}
				catch (Bivrost.LicenseNinja.LicenseException err)
				{
					IsValid = false;
				}
			});
		}

		public void Validate()
		{
			CanValidate = false;
			NotifyOfPropertyChange(null);

			Task.Factory.StartNew(async () => {
				try
				{
					long seconds = await Bivrost.LicenseNinja.Verify(Logic.Instance.settings.ProductCode, LicenseCode, Logic.Instance.settings.InstallId.ToString());
					Logic.Instance.settings.LicenseCode = LicenseCode;
					Logic.Instance.settings.Save();
					IsValid = true;
					Execute.OnUIThread(() => TryClose());
				}
				catch (Bivrost.LicenseNinja.LicenseException err)
				{
					Logic.Instance.settings.LicenseCode = "";
					Logic.Instance.settings.Save();

					IsValid = false;
					CanValidate = true;
					LicenseCode = "";
					NotifyOfPropertyChange(null);
				}
				
			});			
		}
#endif
	}
}
