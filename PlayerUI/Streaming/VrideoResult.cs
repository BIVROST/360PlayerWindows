using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace PlayerUI.Streaming
{

	public class VrideoParser : ServiceParser
	{
		public override bool CanParse(string uri)
		{
			if (uri == null) return false;
			return Regex.IsMatch(uri, @"^(https?://)?(www.)?vrideo.com/watch/.+", RegexOptions.IgnoreCase);
		}

		internal string UriToId(string uri)
		{
			var match = Regex.Match(uri, @"vrideo.com/watch/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
			return match.Groups[1].Captures[0].Value;
		}

		internal MediaDecoder.ProjectionMode ParseProjection(string projection)
		{
			switch (projection)
			{
				case "sphere": return MediaDecoder.ProjectionMode.Sphere;
				case "sphere-180-equirect":
				case "sphere-180-fisheye":
				case "cylinder":
				case "cylinder-stacked":
				case "plane":
					throw new StreamNotSupported("Projection not supported yet: " + projection);
				default:
					throw new StreamParsingFailed("Projection unknown: " + projection);
			}
		}

		internal VideoQuality? ParseQuality(string quality)
		{
			switch (quality)
			{
				case "480p": return null;
				case "720p": return VideoQuality.hdready;
				case "1080p": return VideoQuality.fullhd;
				case "2k": return VideoQuality.q2k;
				case "4k": return VideoQuality.q4k;
			};
			throw new StreamParsingFailed("Quality unknown: " + quality);
		}

		internal MediaDecoder.VideoMode ParseStereo(string stereo)
		{
			switch(stereo) {
				case "not3d":	return MediaDecoder.VideoMode.Mono;
				case "sbs_left_on_left": return MediaDecoder.VideoMode.SideBySide;
				case "sbs_right_on_left": return MediaDecoder.VideoMode.SideBySideReversed;
				case "t2b_left_on_top": return MediaDecoder.VideoMode.TopBottom;
				case "t2b_right_on_top": return MediaDecoder.VideoMode.TopBottomReversed;
			}
			throw new StreamParsingFailed("Unknown stereoscopy: " + stereo);
		}

		public override ServiceResult Parse(string url)
		{
			var result = new ServiceResult() { originalURL = url, serviceName = "Vrideo" };
			string id = UriToId(url);
			string jsonString = HTTPGetString("http://www.vrideo.com/api/v1/videos?video_ids=" + id);
			JObject metadata = (JObject)JObject.Parse(jsonString)["items"][0];
			result.projection = ParseProjection((string)metadata["projection"]);
			result.stereoscopy = ParseStereo((string)metadata["stereo_video"]);
			result.title = (string)metadata["title"];

			foreach (JProperty format in metadata["attributes"]["available_format_details"].Children<JProperty>())
			{
				VideoContainer container = (VideoContainer)Enum.Parse(typeof(VideoContainer), format.Name);
				foreach (JToken q in format.Value.Children<JToken>())
				{
					var quality = ParseQuality((string)q);
					if (!quality.HasValue)
					{
						Warn("Ignoring very low quality: " + (string)q);
						continue;
					}
					var urlKey = "video_file_path_" + format.Name + "_" + (string)q;
					VideoStream video = new VideoStream()
					{
						container = container,
						quality = quality.Value,
						url = (string)metadata["attributes"][urlKey],
						hasAudio = true
					};
					if (video.url == null)
					{
						Warn("no video url?");
					}
					else
						result.VideoStreams.Add(video);
				}
			}

			return result;
		}

	}

}