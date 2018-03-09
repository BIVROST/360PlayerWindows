using System.IO;
using Bivrost.Log;

using Bivrost.Bivrost360Player;

namespace Bivrost.AnalyticsForVR
{
	public class LocallyStoredSessionSink : ISessionSink
	{
		private static Logger log = new Logger("Locally stored sessions");
		

		public string DestinationDirectory {
			get {
				return !string.IsNullOrWhiteSpace(Logic.Instance.settings.LocallyStoredSessionsDirectory)
					? Logic.Instance.settings.LocallyStoredSessionsDirectory
					: Logic.LocalDataDirectory;
			}
			set
			{
				Logic.Instance.settings.LocallyStoredSessionsDirectory = value;
				Logic.Instance.settings.Save();
				log.Info($"changed destination folder to {DestinationDirectory}");
			}
		}

		public bool Enabled
		{
			get
			{
				return Logic.Instance.settings.LocallyStoredSessions && Features.LocallyStoredSessions;
			}
		}

		public void UseSession(Session session)
		{
			var dest = $"{DestinationDirectory}/session-{session.time_start.ToString("yyyy-MM-ddTHHmmss")}.360Session";
			File.WriteAllText(
				dest,
				session.ToJson()
			);
			log.Info($"saved a session to {dest}");
		}
	}
}
