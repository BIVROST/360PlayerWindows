using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace PlayerUI.Streaming
{
	public class PornhubParser : ServiceParser
	{

        public override string ServiceName
        {
            get { return "PornHub"; }
        }

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
			var result = new ServiceResult(uri, ServiceName, URIToMediaId(uri));
			string html = HTTPGetString(uri);

			//var match = Regex.Match(html, @"vrProps\s*=\s*({[^;]+})\s*;");
			var match = Regex.Match(html, "({[^{}]*\"stereoSrc\"[^{}]*})");
			if (match.Captures.Count == 0)
				throw new StreamNotSupported("This movie is not VR enabled: " + uri);
			//throw new StreamParsingFailed("vrProps info not found");
			string vrPropsString=match.Groups[1].Captures[0].Value;
			JObject vrProps = JObject.Parse(vrPropsString);

			if(!(bool)vrProps["enabled"])
				throw new StreamNotSupported("This movie is not VR enabled: " + uri);

			result.projection = ParseProjection((int)vrProps["projection"]);
			result.stereoscopy = (bool)vrProps["stereoSrc"] ? ParseStereo((int)vrProps["stereoType"]) : MediaDecoder.VideoMode.Mono;

			result.title = Regex.Match(html, @"<title>(.+)<[/]title>").Groups[1].Captures[0].Value;

			foreach (var p in new Tuple<string, int, int, int>[] {
				Tuple.Create("720p", 1, 1280, 720),
				Tuple.Create("1080p", 2, 1920, 1080),
				Tuple.Create("2k", 3, 2048, 1024),
				Tuple.Create("4k", 4, 3840, 2160)
			}) { 
				var matches=Regex.Match(html, "var\\s+player_quality_" + p.Item1 + "\\s*=\\s*'([^;]+)'\\s*;");
				if (matches.Captures.Count > 0)
					result.videoStreams.Add(new VideoStream()
					{
						container = VideoContainer.mp4,
						quality = p.Item2,
						width = p.Item3,
						height = p.Item4,
						url = matches.Groups[1].Captures[0].Value,
						hasAudio = true
					});
			}
			if (result.videoStreams.Count == 0)
				throw new StreamParsingFailed("No videos found");
			
			return result;
		}
	}
}
