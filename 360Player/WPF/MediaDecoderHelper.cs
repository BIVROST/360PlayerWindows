using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.WPF
{
	public static class MediaDecoderHelper
	{
		public static NotificationViewModel GetNotification(MediaDecoder.Error error)
		{
			var majorErr = (SharpDX.MediaFoundation.MediaEngineErr)error.major;
			return GetNotification(majorErr);
		}

		public static NotificationViewModel GetNotification(SharpDX.MediaFoundation.MediaEngineErr errorType)
		{
			var notification = new NotificationViewModel(ParseError(errorType), GetHelpUrl(errorType), Timeout(errorType));
			return notification;
		}

		private static string ParseError(SharpDX.MediaFoundation.MediaEngineErr errorType) { 
			switch (errorType)
			{
				case SharpDX.MediaFoundation.MediaEngineErr.Aborted: return "Media playback was aborted.";
				case SharpDX.MediaFoundation.MediaEngineErr.Decode: return "An error occured while decoding the media resource.";
				case SharpDX.MediaFoundation.MediaEngineErr.Encrypted: return "An error occured while encrypting the media resource.";
				case SharpDX.MediaFoundation.MediaEngineErr.Network: return "An network error occured.";
				case SharpDX.MediaFoundation.MediaEngineErr.SourceNotSupported: return "Selected media source is not supported.";
				case SharpDX.MediaFoundation.MediaEngineErr.Noerror: return "There was no error. Strange...";
				default: return "Something went wrong...";
			}
		}

		private static string GetHelpUrl(SharpDX.MediaFoundation.MediaEngineErr errorType)
		{
			switch (errorType)
			{
				case SharpDX.MediaFoundation.MediaEngineErr.SourceNotSupported: return "http://bivrost360.com/desktop/help/formats";
				
				default: return "";
			}
		}

		public static float Timeout(SharpDX.MediaFoundation.MediaEngineErr errorType)
		{
			return 5f;
		}
	}
}
