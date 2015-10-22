using GoogleAnalyticsTracker.Core;
using GoogleAnalyticsTracker.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class GoogleTracker : TrackerBase
	{

		public GoogleTracker(string trackingAccount, string trackingDomain, ITrackerEnvironment trackerEnvironment) 
			: base(trackingAccount, trackingDomain, trackerEnvironment)
		{
			
		}

		public GoogleTracker(string trackingAccount, string trackingDomain, IAnalyticsSession analyticsSession, ITrackerEnvironment trackerEnvironment) 
			: base(trackingAccount, trackingDomain, analyticsSession, trackerEnvironment)
		{

		}

	}
}
