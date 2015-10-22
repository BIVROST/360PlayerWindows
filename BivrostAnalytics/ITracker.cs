using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BivrostAnalytics
{
	interface ITracker
	{
		void TrackEvent();
		void TrackPage();
	}
}
