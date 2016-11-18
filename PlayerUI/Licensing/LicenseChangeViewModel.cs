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

		private System.Action<string> validateCallback = null;
		private System.Action clearLicenseCallback = null;
		private LicenseManagementViewModel.LicenceChangeReason reason;


		public LicenseChangeViewModel(LicenseManagementViewModel.LicenceChangeReason reason, string oldLicense, Action<string> openLicenseVerify, System.Action licenseClear)
		{
			this.LicenseCode = oldLicense;
			this.reason = reason;
			this.validateCallback = openLicenseVerify;
			this.clearLicenseCallback = licenseClear;
		}

		//public bool CanValidate { get { return !string.IsNullOrWhiteSpace(_licenseCode); } }

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
					case LicenseManagementViewModel.LicenceChangeReason.explicitChange:
						return "Enter license key.";

					case LicenseManagementViewModel.LicenceChangeReason.licenseEnded:
						return "Your license has ended.";

					case LicenseManagementViewModel.LicenceChangeReason.licenseUnknown:
						return "Your license number is invalid.";

					default:
						Logger.Error("Unknown license change reason: " + reason);
						goto case LicenseManagementViewModel.LicenceChangeReason.explicitChange;
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
