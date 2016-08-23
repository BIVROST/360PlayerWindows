using Bivrost.Log;
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
				case "sphere":
					return MediaDecoder.ProjectionMode.Sphere;
				case "sphere-180-equirect":
					return MediaDecoder.ProjectionMode.Dome;
				case "sphere-180-fisheye":
				case "cylinder":
				case "cylinder-stacked":
				case "plane":
					throw new StreamNotSupported("Projection not supported yet: " + projection);
				default:
					throw new StreamParsingFailed("Projection unknown: " + projection);
			}
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
					int width, height, quality;
					switch((string)q)
					{
						case "480p":
							Logger.Info("Ignoring very low quality: " + (string)q);
							continue;

						case "720p":
							quality = 1;  width = 1280; height = 720;
							break;

						case "1080p":
							quality = 2; width = 1920; height = 1080;
							break;

						case "2k":
							quality = 3; width = 2048; height = 1024;
							break;

						case "4k":
							quality = 4; width = 3840; height = 2160;
							break;

						default:
							Logger.Error("unknown quality: " + q);
							continue;
					}

					var urlKey = "video_file_path_" + format.Name + "_" + (string)q;
					VideoStream video = new VideoStream()
					{
						container = container,
						quality = quality,
						width = width,
						height = height,
						url = (string)metadata["attributes"][urlKey],
						hasAudio = true
					};
					if (video.url == null)
					{
                        Logger.Error("no video url?");
					}
					else
						result.videoStreams.Add(video);
				}
			}

			return result;
		}

	}

}