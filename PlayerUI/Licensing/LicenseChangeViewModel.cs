using Bivrost.Log;
using Caliburn.Micro;
using System;

namespace PlayerUI.Licensing
{
	public class LicenseChangeViewModel : PropertyChangedBase
	{

		private string _licenseCode = "";
		public string LicenseCode
		{
			get
			{
				return _licenseCode;
			}
			set
			{
				Console.WriteLine("licenseCode " + value);
				_licenseCode = value;
				NotifyOfPropertyChange(() => LicenseCode);
				//NotifyOfPropertyChange(() => CanValidate);
			}
		}

		private LicenseManagementViewModel.LicenseChangeReason reason;


		public LicenseChangeViewModel(LicenseManagementViewModel.LicenseChangeReason reason, string oldLicense, Action<string> openLicenseVerify, System.Action licenseClear)
		{
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
						LoggerManager.Error("Unknown license change reason: " + reason);
						goto case LicenseManagementViewModel.LicenseChangeReason.explicitChange;
				}
			}
		}


		public System.Windows.Visibility ClearLicenseVisible
		{
			get
			{
				return Features.RequireLicense
					? System.Windows.Visibility.Hidden
					: System.Windows.Visibility.Visible;
			}
		}

	}
}
