using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BivrostAnalytics
{
	public class TrackerParameters
	{
		public class GeneralParameters
		{
			/// <summary>
			/// The Protocol version. The current value is '1'. This will only change when there are changes made that are not backwards compatible.
			/// 
			/// Required for all hit types.
			/// 
			/// Example value: 1
			/// Example usage: v=1
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "v", Required = true)]
			public string ProtocolVersion { get; set; }

			/// <summary>
			/// The tracking ID / web property ID. The format is UA-XXXX-Y. All collected data is associated by this ID.
			/// 
			/// Required for all hit types.
			/// 
			/// Example value: UA-XXXX-Y
			/// Example usage: tid=UA-XXXX-Y
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "tid", Required = true)]
			public string TrackingID { get; set; }

			[ProtocolVariableAttribute(QueryString = "aip")]
			public string AnonymizeIP { get; set; }

			[ProtocolVariableAttribute(QueryString = "ds")]
			public string DataSource { get; set; }

			[ProtocolVariableAttribute(QueryString = "qt")]
			public Int64? QueueTime { get; set; }

			[ProtocolVariableAttribute(QueryString = "z")]
			public Int64? CacheBuster { get; set; }			
        }

		public class UserParameters
		{
			[ProtocolVariableAttribute(QueryString = "cid", Required = true)]
			public string ClientID { get; set; }

			[ProtocolVariableAttribute(QueryString = "uid")]
			public string UserID { get; set; }
		}

		public class SessionParameters
		{
			[ProtocolVariableAttribute(QueryString = "sc")]
			public SessionControlCommand SessionControl { get; set; }

			/// <summary>
			/// IP format 1.2.3.4
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "uip")]
			public string IPOverride { get; set; }

			[ProtocolVariableAttribute(QueryString = "ua")]
			public string UserAgentOverride { get; set; }

			/// <summary>
			/// geoid=US
			/// geoid=21137
			/// http://developers.google.com/analytics/devguides/collection/protocol/v1/geoid
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "geoid")]
			public string GeographicalOverride { get; set; }
		}

		
		public class TrafficSourcesParameters
		{
			/// <summary>
			/// Max length 2048 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dr", ByteLimit = 2048)]
			public string DocumentReferrer { get; set; }

			/// <summary>
			/// Max length 100 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cn", ByteLimit = 100)]
			public string CampaignName { get; set; }

			/// <summary>
			/// Max length 100 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cs", ByteLimit = 100)]
			public string CampaignSource { get; set; }

			/// <summary>
			/// Max length 50 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cm", ByteLimit = 50)]
			public string CampaignMedium { get; set; }

			/// <summary>
			/// Max length 500 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ck", ByteLimit = 500)]
			public string CampaignKeyword { get; set; }

			/// <summary>
			/// Max length 500 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cc", ByteLimit = 500)]
			public string CampaignContent { get; set; }

			/// <summary>
			/// Max length 100 bytes
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ci", ByteLimit = 100)]
			public string CampaignID { get; set; }

			[ProtocolVariableAttribute(QueryString = "gclid")]
			public string GoogleAdwordsID { get; set; }

			[ProtocolVariableAttribute(QueryString = "dclid")]
			public string GoogleDisplayAdsID { get; set; }
		}

		public class SystemInfoParameters
		{
			/// <summary>
			/// Format 800x600
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "sr", ByteLimit = 20)]
			public string ScreenResolution { get; set; }

			/// <summary>
			/// Format 123x456
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "vp", ByteLimit = 20)]
			public string ViewportSize { get; set; }

			[ProtocolVariableAttribute(QueryString = "de", ByteLimit = 20)]
			public string DocumentEncoding { get; set; } = "UTF-8";

			/// <summary>
			/// Example value: 24-bits
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "sd", ByteLimit = 20)]
			public string ScreenColors { get; set; }

			/// <summary>
			/// Example value: en-us
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ul", ByteLimit = 20)]
			public string UserLanguage { get; set; }

			[ProtocolVariableAttribute(QueryString = "je")]
			public bool? JavaEnabled { get; set; }

			/// <summary>
			/// Example value: 10 1 r103
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "fl", ByteLimit = 20)]
			public string FlashVersion { get; set; }
		}

		public class HitParameters
		{
			/// <summary>
			/// The type of hit. Must be one of 'pageview', 'screenview', 'event', 'transaction', 'item', 'social', 'exception', 'timing'.
			/// Example value: pageview
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "t")]
			public HitType HitType { get; set; }

			[ProtocolVariableAttribute(QueryString = "ni")]
			public bool? NonInteractionHit { get; set; }
		}

		public class ContentInformationParameters
		{
			/// <summary>
			/// Example value: http://foo.com/home?a=b
			/// Example usage: dl=http%3A%2F%2Ffoo.com%2Fhome%3Fa%3Db
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dl", ByteLimit = 2048)]
			public string DocumentLocationURL { get; set; }

			/// <summary>
			/// Example value: foo.com
			/// Example usage: dh=foo.com
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dh", ByteLimit = 100)]
			public string DocumentHostName { get; set; }

			/// <summary>
			/// Example value: /foo
			/// Example usage: dp=%2Ffoo
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dp", ByteLimit = 2048)]
			public string DocumentPath { get; set; }

			/// <summary>
			/// Example value: Settings
			/// Example usage: dt=Settings
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dt", ByteLimit = 1500)]
			public string DocumentTitle { get; set; }

			/// <summary>
			/// Example value: High Scores
			/// Example usage: cd=High%20Scores
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cd", ByteLimit = 2048, Required = true)]
			public string ScreenName { get; set; }

			/// <summary>
			/// Example value: nav_bar
			/// Example usage: linkid=nav_bar
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "linkid")]
			public string LinkId { get; set; }
		}

		public class AppTrackingParameters
		{
			/// <summary>
			/// Example value: My App
			/// Example usage: an=My%20App
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "an", ByteLimit = 100, Required = true)]
			public string ApplicationName { get; set; }

			/// <summary>
			/// Example value: com.company.app
			/// Example usage: aid=com.company.app
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "aid", ByteLimit = 150)]
			public string ApplicationID { get; set; }

			/// <summary>
			/// Example value: 1.2
			/// Example usage: av=1.2
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "av", ByteLimit = 100)]
			public string ApplicationVersion { get; set; }

			/// <summary>
			/// Example value: com.platform.vending
			/// Example usage: aiid=com.platform.vending
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "aiid", ByteLimit = 150)]
			public string ApplicationInstallerID { get; set; }
		}

		public class EventTrackingParameters
		{
			/// <summary>
			/// Specifies the event action. Must not be empty.
			/// 
			/// Example value: Category
			/// Example usage: ec=Category
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ec", ByteLimit = 150, Required = true)]
			public string EventCategory { get; set; }

			/// <summary>
			/// Specifies the event category. Must not be empty.
			/// 
			/// Example value: Action
			/// Example usage: ea=Action
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ea", ByteLimit = 500, Required = true)]
			public string EventAction { get; set; }

			/// <summary>
			/// Specifies the event category. Must not be empty.
			/// 
			/// Example value: Label
			/// Example usage: el=Label
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "el", ByteLimit = 500)]
			public string EventLabel { get; set; }

			/// <summary>
			/// Example value: 55
			/// Example usage: ev=55
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "ev")]
			public Int64? EventValue { get; set; }
		}

		public class ECommerceParameters
		{
			//TODO zaimplementowac brakujace parametry
		}

		public class EnhancedECommerceParameters
		{
			//TODO zaimplementowac brakujace parametry
		}

		public class SocialInteractionsParameters
		{
			/// <summary>
			/// Specifies the social network, for example Facebook or Google Plus.
			/// Required for social hit type.
			/// 
			/// Example value: facebook
			/// Example usage: sn=facebook
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "sn", ByteLimit = 50, Required = true)]
			public Int64? SocialNetwork { get; set; }

			/// <summary>
			/// Specifies the social interaction action. For example on Google Plus when a user clicks the +1 button, the social action is 'plus'.
			/// Required for social hit type.
			/// 
			/// Example value: like
			/// Example usage: sa=like
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "sa", ByteLimit = 50, Required = true)]
			public Int64? SocialAction { get; set; }

			/// <summary>
			/// Specifies the target of a social interaction. This value is typically a URL but can be any text.
			/// Required for social hit type.
			/// 
			/// Example value: http://foo.com
			/// Example usage: st=http%3A%2F%2Ffoo.com
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "st", ByteLimit = 2048, Required = true)]
			public Int64? SocialActionTarget { get; set; }

		}

		public class TimingParameters
		{
			/// <summary>
			/// Example value: category
			/// Example usage: utc=category
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "utc", ByteLimit = 150, Required = true)]
			public Int64? UserTimingCategory { get; set; }

			/// <summary>
			/// Example value: lookup
			/// Example usage: utv=lookup
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "utv", ByteLimit = 500, Required = true)]
			public Int64? UserTimingVariableName { get; set; }

			/// <summary>
			/// Specifies the user timing value. The value is in milliseconds.
			/// 
			/// Example value: 123
			/// Example usage: utt=123
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "utt", Required = true)]
			public Int64? UserTimingTime { get; set; }

			/// <summary>
			/// Example value: label
			/// Example usage: utl=label
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "utl", ByteLimit = 500)]
			public string UserTimingLabel { get; set; }

			/// <summary>
			/// Example value: 3554
			/// Example usage: plt=3554
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "plt")]
			public Int64? PageLoadTime { get; set; }

			/// <summary>
			/// Specifies the time it took to do a DNS lookup.The value is in milliseconds.
			/// 
			/// Example value: 43
			/// Example usage: dns=43
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dns")]
			public Int64? DNSTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: pdt=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "pdt")]
			public Int64? PageDownloadTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: rrt=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "rrt")]
			public Int64? RedirectResponseTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: tcp=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "tcp")]
			public Int64? TCPConnectTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: srt=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "srt")]
			public Int64? ServerResponseTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: dit=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "dit")]
			public Int64? DomInteractiveTime { get; set; }

			/// <summary>
			/// Example value: 500
			/// Example usage: clt=500
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "clt")]
			public Int64? ContentLoadTime { get; set; }
		}


		public class ExceptionsParameters
		{
			/// <summary>
			/// Example value: DatabaseError
			/// Example usage: exd=DatabaseError
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "exd", ByteLimit = 150)]
			public string ExceptionDescription { get; set; }

			/// <summary>
			/// Example value: DatabaseError
			/// Example usage: exd=DatabaseError
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "exf")]
			public bool? IsExceptionFatal { get; set; } = true;
		}

		public class CustomDimensionsMetricsParameters
		{
			/// <summary>
			/// Example value: Sports
			/// Example usage: cd<dimensionIndex>=Sports
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cd{0}", ByteLimit = 150)]
			public Dictionary<int,string> CustomDimention { get; set; }

			/// <summary>
			/// Example value: 47
			/// Example usage: cm<metricIndex>=47
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "cm{0}")]
			public Dictionary<int, int> CustomMetric { get; set; }
		}

		public class ContentExperiments
		{
			/// <summary>
			/// This parameter specifies that this user has been exposed to an experiment with the given ID. It should be sent in conjunction with the Experiment Variant parameter.
			/// 
			/// Example value: Qp0gahJ3RAO3DJ18b0XoUQ
			/// Example usage: xid=Qp0gahJ3RAO3DJ18b0Xo
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "xid", ByteLimit = 40)]
			public string ExperimentID { get; set; }

			/// <summary>
			/// This parameter specifies that this user has been exposed to a particular variation of an experiment. It should be sent in conjunction with the Experiment ID parameter.
			/// 
			/// Example value: 1
			/// Example usage: xvar=1
			/// </summary>
			[ProtocolVariableAttribute(QueryString = "xvar")]
			public string ExperimentVariant { get; set; }
		}

    }
	
}
