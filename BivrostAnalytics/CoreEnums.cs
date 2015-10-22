using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BivrostAnalytics
{
	public enum SessionControlCommand
	{
		Start,
		End
	}

	public enum HitType
	{
		Pageview,
		Screenview,
		Event,
		Transaction,
		Item,
		Social,
		Exception,
		Timing
	}

}
