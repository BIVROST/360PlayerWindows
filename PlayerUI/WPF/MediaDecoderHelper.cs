using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.WPF
{
	public static class MediaDecoderHelper
	{
		public static NotificationViewModel GetNotification(MediaDecoder.Error error)
		{
			var notification = new NotificationViewModel(ParseError(error), GetHelpUrl(error), Timeout(error));
			return notification;
		}

		public static string ParseError(MediaDecoder.Error error)
		{
			var errorType = (SharpDX.MediaFoundation.MediaEngineErr)error.major;
			switch(errorType)
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

		public static string GetHelpUrl(MediaDecoder.Error error)
		{
			var errorType = (SharpDX.MediaFoundation.MediaEngineErr)error.major;
			switch (errorType)
			{
				case SharpDX.MediaFoundation.MediaEngineErr.SourceNotSupported: return "http://bivrost360.com/desktop/help/formats";
				
				default: return "";
			}
		}

		public static float Timeout(MediaDecoder.Error error)
		{
			return 5f;
		}
	}
}
