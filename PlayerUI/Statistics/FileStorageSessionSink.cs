using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Statistics
{
	class FileStorageSessionSink : ISessionSink
	{
		public bool Enabled
		{
			get
			{
				return Logic.Instance.settings.LocalHeatmaps;
			}
		}

		public void UseSession(Session session)
		{
			System.IO.File.WriteAllText(
				$"{Logic.LocalDataDirectory}/session-{session.time_start.ToString("yyyy-MM-ddTHHmmss")}.360Session",
				session.ToJson()
			);
		}
	}
}
