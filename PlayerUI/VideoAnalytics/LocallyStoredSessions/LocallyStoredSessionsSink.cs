using System.IO;
using Bivrost.Log;

namespace PlayerUI.VideoAnalytics
{
	public class LocallyStoredSessionSink : ISessionSink
	{
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
				Logger.Info($"[LocallyStoredSessions]: changed destination folder to {DestinationDirectory}");
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
			Logger.Info($"[LocallyStoredSessions]: saved a session to {dest}");
		}
	}
}
