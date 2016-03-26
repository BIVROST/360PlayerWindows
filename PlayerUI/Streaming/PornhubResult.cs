using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace PlayerUI.Streaming
{
	public class PornhubParser : ServiceParser
	{
		public override bool CanParse(string uri)
		{
			if (uri == null) return false;
			return Regex.IsMatch(uri, @"^(https?://)?(www.)?pornhub.com/view_video.php?.+", RegexOptions.IgnoreCase);
		}

		internal MediaDecoder.ProjectionMode ParseProjection(int projection)
		{
			switch (projection)
			{
				case 1: throw new StreamNotSupported("DomeEquidistant projection not supported yet: " + projection);
				case 2: return MediaDecoder.ProjectionMode.Sphere;
				case 3: return MediaDecoder.ProjectionMode.Dome;
				default: throw new StreamParsingFailed("Projection unknown: " + projection);
			}
		}

		internal MediaDecoder.VideoMode ParseStereo(int stereo)
		{
			switch (stereo)
			{
				case 1: return MediaDecoder.VideoMode.SideBySide;
				case 2: return MediaDecoder.VideoMode.TopBottom;
				case 3: return MediaDecoder.VideoMode.SideBySideReversed;
				case 4: return MediaDecoder.VideoMode.TopBottomReversed;
				default:
					throw new StreamParsingFailed("Stereoscopy unknown: " + stereo);
			}
		}


		public override ServiceResult Parse(string uri)
		{
			var result = new ServiceResult() { originalURL = uri, serviceName = "Pornhub" };
			string html = HTTPGetString(uri);

			//var match = Regex.Match(html, @"vrProps\s*=\s*({[^;]+})\s*;");
			var match = Regex.Match(html, "({[^{}]*\"stereoSrc\"[^{}]*})");
			if (match.Captures.Count == 0)
				throw new StreamNotSupported("This movie is not VR enabled");
			//throw new StreamParsingFailed("vrProps info not found");
			string vrPropsString=match.Groups[1].Captures[0].Value;
			JObject vrProps = (JObject)JObject.Parse(vrPropsString);

			if(!(bool)vrProps["enabled"])
				throw new StreamNotSupported("This movie is not VR enabled");

			result.projection = ParseProjection((int)vrProps["projection"]);
			result.stereoscopy = (bool)vrProps["stereoSrc"] ? ParseStereo((int)vrProps["stereoType"]) : MediaDecoder.VideoMode.Mono;

			result.title = Regex.Match(html, @"<title>(.+)<[/]title>").Groups[1].Captures[0].Value;

			foreach (var p in new Tuple<string, VideoQuality>[] {
				Tuple.Create("720p", VideoQuality.hdready),
				Tuple.Create("1080p", VideoQuality.fullhd),
				Tuple.Create("2k", VideoQuality.q2k),
				Tuple.Create("4k", VideoQuality.q4k)
			}) {
				var matches=Regex.Match(html, "var\\s+player_quality_" + p.Item1 + "\\s*=\\s*'([^;]+)'\\s*;");
				if (matches.Captures.Count > 0)
					result.VideoStreams.Add(new VideoStream()
					{
						container = VideoContainer.mp4,
						quality = p.Item2,
						url = matches.Groups[1].Captures[0].Value,
						hasAudio = true
					});
			}
			if (result.VideoStreams.Count == 0)
				throw new StreamParsingFailed("No videos found");
			
			return result;
		}
	}
}
