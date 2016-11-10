using Bivrost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlayerUI.Streaming
{

	public class BivrostProtocolParser : ServiceParser
	{

		public override bool CanParse(string url)
		{
			var uri = new Uri(url);
			return uri.Scheme == "bivrost";
		}

		public override ServiceResult Parse(string uri)
		{
			var protocol = Protocol.Parse(uri);
			MediaDecoder.VideoMode stereoscopy;
			switch (protocol.stereoscopy)
			{
				default:
				case Protocol.Stereoscopy.autodetect: stereoscopy = MediaDecoder.VideoMode.Autodetect; break;
				case Protocol.Stereoscopy.mono: stereoscopy = MediaDecoder.VideoMode.Mono; break;
				case Protocol.Stereoscopy.side_by_side: stereoscopy = MediaDecoder.VideoMode.SideBySide; break;
				case Protocol.Stereoscopy.top_and_bottom: stereoscopy = MediaDecoder.VideoMode.TopBottom; break;
				case Protocol.Stereoscopy.top_and_bottom_reversed: stereoscopy = MediaDecoder.VideoMode.TopBottomReversed; break;
			}
			bool Loop = protocol.loop.HasValue ? protocol.loop.Value : false;       // unused
			string videoUrl = protocol.urls.FirstOrDefault((u) =>
			{
				var b1 = Regex.IsMatch(u, @"(\b|_).mp4(\b|_)");
				var b2 = Regex.IsMatch(u, @"(\b|_).avi(\b|_)");
				return b1 || b2;
			});
			if (string.IsNullOrWhiteSpace(videoUrl))
				videoUrl = protocol.urls[0];

			if (string.IsNullOrWhiteSpace(videoUrl))
				throw new StreamParsingFailed("no video stated in protocol");

			MediaDecoder.ProjectionMode projection;
			switch (protocol.projection)
			{
				default:
				case Protocol.Projection.equirectangular: projection = MediaDecoder.ProjectionMode.Sphere; break;
			}

			string title = System.IO.Path.GetFileName(videoUrl);
			if (string.IsNullOrWhiteSpace(title))
				title = uri;

			return new ServiceResult()
			{
				originalURL = uri,
				projection = projection,
				stereoscopy = stereoscopy,
				serviceName = "Bivrost protocol",
				title = title,
				videoStreams = new List<VideoStream>()
				{
					new VideoStream()
					{
						url = videoUrl,
						hasAudio = true, // TODO
                        container = GuessContainerFromExtension(videoUrl)
                    }
				}
			};
		}
	}
}
