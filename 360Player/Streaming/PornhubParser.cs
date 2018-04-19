using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Bivrost.Bivrost360Player.Streaming
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

		internal ProjectionMode ParseProjection(int projection)
		{
			switch (projection)
			{
				case 1: throw new StreamNotSupported("DomeEquidistant projection not supported yet: " + projection);
				case 2: return ProjectionMode.Sphere;
				case 3: return ProjectionMode.Dome;
				default: throw new StreamParsingFailed("Projection unknown: " + projection);
			}
		}

		internal VideoMode ParseStereo(int stereo)
		{
			switch (stereo)
			{
				case 1: return VideoMode.SideBySide;
				case 2: return VideoMode.TopBottom;
				case 3: return VideoMode.SideBySideReversed;
				case 4: return VideoMode.TopBottomReversed;
				default:
					throw new StreamParsingFailed("Stereoscopy unknown: " + stereo);
			}
		}


		public override ServiceResult Parse(string uri)
		{
			var result = new ServiceResult(uri, ServiceName, URIToMediaId(uri));
			string html = HTTPGetString(uri);

			//var match = Regex.Match(html, @"vrProps\s*=\s*({[^;]+})\s*;");
			//var match = Regex.Match(html, "({[^{}]*\"stereoSrc\"[^{}]*})");

			var match = Regex.Match(html, @"^\s*vrProps\s*=\s*(.+),\s*$", RegexOptions.Multiline);



			if (match.Captures.Count == 0)
				throw new StreamNotSupported("This movie is not VR enabled: " + uri);
			//throw new StreamParsingFailed("vrProps info not found");
			string vrPropsString=match.Groups[1].Captures[0].Value;
			JObject vrProps = JObject.Parse(vrPropsString);

			if(!(bool)vrProps["enabled"])
				throw new StreamNotSupported("This movie is not VR enabled: " + uri);

			result.projection = ParseProjection((int)vrProps["projection"]);
			result.stereoscopy = (bool)vrProps["stereoSrc"] ? ParseStereo((int)vrProps["stereoType"]) : VideoMode.Mono;

			result.title = Regex.Match(html, @"<title>(.+)<[/]title>").Groups[1].Captures[0].Value;

			//foreach (var p in new Tuple<string, int, int, int>[] {
			//	Tuple.Create("720p", 1, 1280, 720),
			//	Tuple.Create("1080p", 2, 1920, 1080),
			//	Tuple.Create("2k", 3, 2048, 1024),
			//	Tuple.Create("4k", 4, 3840, 2160)
			//}) { 
			//	var matches=Regex.Match(html, "var\\s+player_quality_" + p.Item1 + "\\s*=\\s*'([^;]+)'\\s*;");
			//	if (matches.Captures.Count > 0)
			//		result.videoStreams.Add(new VideoStream()
			//		{
			//			container = VideoContainer.mp4,
			//			quality = p.Item2,
			//			width = p.Item3,
			//			height = p.Item4,
			//			url = matches.Groups[1].Captures[0].Value,
			//			hasAudio = true
			//		});
			//}

			var flashvars = Regex.Match(html, @"^\s*var\s+flashvars_\d+\s*=\s*(.+);\s*$", RegexOptions.Multiline);
			if (flashvars.Captures.Count > 0)
			{
				var json = flashvars.Groups[1].Captures[0].Value;
				//var o = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

				var converter = new ExpandoObjectConverter();
				dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(json, converter);

				foreach(var md in message.mediaDefinitions)
				{
					var stream = new VideoStream()
					{
						container = VideoContainer.mp4,
						url = md.videoUrl,
						hasAudio = true,
						quality = 0,
						width = 0,
						height = 0
					};
					switch (md.quality)
					{
						case "720":
							stream.width = 1280;
							stream.height = 720;
							stream.quality = 1;
							break;
						case "1080":
							stream.width = 1920;
							stream.height = 1080;
							stream.quality = 2;
							break;
						case "2k":
							stream.width = 2048;
							stream.height = 1024;
							stream.quality = 3;
							break;
						case "4k":
							stream.width = 3840;
							stream.height = 2160;
							stream.quality = 4;
							break;
					}
					result.videoStreams.Add(stream);
				}
			}


			if (result.videoStreams.Count == 0)
				throw new StreamParsingFailed("No videos found");
			
			return result;
		}
	}
}
