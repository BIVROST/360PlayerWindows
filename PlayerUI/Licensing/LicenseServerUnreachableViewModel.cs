using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Licensing
{
	public class LicenseServerUnreachableViewModel : PropertyChangedBase
	{

		public LicenseServerUnreachableViewModel(System.Action useBasicFeaturesCallback, System.Action retryCallback)
		{
			this.useBasicFeaturesCallback = useBasicFeaturesCallback;
			this.retryCallback = retryCallback;
		}


		private System.Action useBasicFeaturesCallback;
		public void UseBasicFeaturesCallback() { useBasicFeaturesCallback(); }


		private System.Action retryCallback;
		public void RetryCallback() { retryCallback(); }


		public bool ClearLicenseVisible { get { return !Features.RequireLicense; } }

	}
}
