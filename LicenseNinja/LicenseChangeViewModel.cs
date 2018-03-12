using Bivrost.Log;
using Caliburn.Micro;
using System;

namespace Bivrost.Licensing
{
	public class LicenseChangeViewModel : PropertyChangedBase
	{
		private string _licenseCode = "";
		private readonly LicensingConnector.IContext context;

		public string LicenseCode
		{
			get
			{
				return _licenseCode;
			}
			set
			{
				_licenseCode = value;
				NotifyOfPropertyChange(() => LicenseCode);
				//NotifyOfPropertyChange(() => CanValidate);
			}
		}

		private LicenseManagementViewModel.LicenseChangeReason reason;


		public LicenseChangeViewModel(LicensingConnector.IContext context, LicenseManagementViewModel.LicenseChangeReason reason, string oldLicense, Action<string> openLicenseVerify, System.Action licenseClear)
		{
			this.context = context;
			this.LicenseCode = oldLicense;
			this.reason = reason;
			this.validateCallback = openLicenseVerify;
			this.clearLicenseCallback = licenseClear;
		}

		//public bool CanValidate { get { return !string.IsNullOrWhiteSpace(_licenseCode); } }


		private System.Action clearLicenseCallback = null;
		public void ClearLicense() { clearLicenseCallback(); }


		private System.Action<string> validateCallback = null;
		public void Validate()
		{
			validateCallback(LicenseCode);
		}


		public string Message
		{
			get
			{
				switch(reason)
				{
					case LicenseManagementViewModel.LicenseChangeReason.explicitChange:
						return "Enter license key.";

					case LicenseManagementViewModel.LicenseChangeReason.licenseEnded:
						return "Your license has ended.";

					case LicenseManagementViewModel.LicenseChangeReason.licenseUnknown:
						return "Your license number is invalid.";

					case LicenseManagementViewModel.LicenseChangeReason.licenseRequired:
						return "A license number is required to use this product.";

					default:
						Bivrost.Licensing.LicenseNinja.log.Error("Unknown license change reason: " + reason);
						goto case LicenseManagementViewModel.LicenseChangeReason.explicitChange;
				}
			}
		}


		public System.Windows.Visibility ClearLicenseVisible
		{
			get
			{
				return context.RequireLicense
					? System.Windows.Visibility.Hidden
					: System.Windows.Visibility.Visible;
			}
		}

	}
}
