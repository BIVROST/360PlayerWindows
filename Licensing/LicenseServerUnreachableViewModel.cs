using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Licensing
{
	public class LicenseServerUnreachableViewModel : PropertyChangedBase
	{

		public LicenseServerUnreachableViewModel(LicensingConnector.IContext context, System.Action useBasicFeaturesCallback, System.Action retryCallback)
		{
			this.context = context;
			this.useBasicFeaturesCallback = useBasicFeaturesCallback;
			this.retryCallback = retryCallback;
		}

		private readonly LicensingConnector.IContext context;
		private System.Action useBasicFeaturesCallback;
		public void UseBasicFeatures() { useBasicFeaturesCallback(); }


		private System.Action retryCallback;
		public void Retry() { retryCallback(); }


		public bool ClearLicenseVisible { get { return !context.RequireLicense; } }

	}
}
