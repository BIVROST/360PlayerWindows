using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleAnalyticsTracker.Simple;
using GoogleAnalyticsTracker.Core.Interface;
using GoogleAnalyticsTracker.Core.TrackerParameters;
using GoogleAnalyticsTracker;
using GoogleAnalyticsTracker.Core;

namespace PlayerUI
{
	public class Stats
	{
		private SimpleTracker tracker;
		private SimpleTrackerEnvironment trackerEnvironment;
		private PlayerSession trackerSession;

		public Stats()
		{
			trackerSession = new PlayerSession();
			//trackerEnvironment = new SimpleTrackerEnvironment()
			//{
			//	Hostname = Environment.MachineName,
			//	OsPlatform = Environment.OSVersion.Platform.ToString(),
			//	OsVersion = Environment.OSVersion.Version.ToString(),
			//	OsVersionString = Environment.OSVersion.VersionString				
			//};
			tracker = new SimpleTracker("UA-68212464-1", "");
			AnalyticsSession session = new AnalyticsSession();
        }

		public async void TrackAppStart()
		{
			var resultEvent = await tracker.TrackAsync(new PlayerParameters(HitType.Event, "Start"));
			var resultPageView = await tracker.TrackPageViewAsync("Start screen", "StartScreen");
		}

		public async void TrackPlay()
		{
			await tracker.TrackAsync(new PlayerParameters(HitType.Event, "Play"));
		}

		public async void TrackStop()
		{
			await tracker.TrackAsync(new PlayerParameters(HitType.Event, "Stop"));
		}

		public async void TrackAppQuit()
		{
			await tracker.TrackAsync(new PlayerParameters(HitType.Event, "Quit"));
		}
	}

	public class PlayerParameters : GoogleAnalyticsTracker.Core.TrackerParameters.GeneralParameters
	{
		public HitType type;

		public PlayerParameters(HitType type, string description)
		{
			this.type = type;
			this.DocumentTitle = description;
		}
		public override HitType HitType
		{
			get
			{
				return type;
			}
		}
	}


	public class PlayerSession : IAnalyticsSession
	{
		private string sessionId;

		public PlayerSession()
		{
			
		}

		public string GenerateCacheBuster()
		{
			throw new NotImplementedException();
		}

		public string GenerateCookieValue()
		{
			throw new NotImplementedException();
		}


		public string GenerateSessionId()
		{
			return sessionId;
		}
	}
}
