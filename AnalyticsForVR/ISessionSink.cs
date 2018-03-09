using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.AnalyticsForVR
{
	public interface ISessionSink
	{
		bool Enabled { get; }

		void UseSession(Session session);
	}

	
}
