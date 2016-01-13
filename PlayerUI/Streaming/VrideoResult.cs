using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace PlayerUI.Streaming
{

	public class VrideoParser : ServiceParser
	{
		public override bool CanParse(string uri)
		{
			if (uri == null) return false;
			return Regex.IsMatch(uri, @"^(https?://)?(www.)?vrideo.com/watch/.+", RegexOptions.IgnoreCase);
		}

		public string UriToId(string uri)
		{
			var match = Regex.Match(uri, @"vrideo.com/watch/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
			return match.Groups[1].Captures[0].Value;
		}

		public MediaDecoder.ProjectionMode ParseProjection(string projection)
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


		public VideoQuality? ParseQuality(string quality)
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

		public async override Task<ServiceResult> TryParse(string url)
		{
			var result = new ServiceResult() { originalURL = url };
			//				var cdn =/\bcdn:\s * "([^"]*)"/.exec(document.body.innerHTML)[1];
			//	 var cdn_dir =/\bcdn_dir:\s * "([^"]*)"/.exec(document.body.innerHTML)[1];
			string id = UriToId(url);
			string jsonString = await HTTPGetStringAsync("http://www.vrideo.com/api/v1/videos?video_ids=" + id);
			JObject metadata = (JObject)JObject.Parse(jsonString)["items"][0];
			result.projection = ParseProjection((string)metadata["projection"]);
			string stereo = (string)metadata["stereo_video"];
			result.title = (string)metadata["title"];

			foreach (var format in (JObject)metadata["attributes"]["available_format_details"])
			{
				VideoContainer container = (VideoContainer)Enum.Parse(typeof(VideoContainer), format.Key);
				foreach (string q in (JArray)format.Value)
				{
					var quality = ParseQuality(q);
					if (!quality.HasValue)
						break; // ignore low quality
					VideoStream video = new VideoStream()
					{
						container = container,
						quality = quality.Value,
						url = (string)metadata["attributes"]["video_file_path_" + quality + "_full." + container],
						hasAudio = true
					};
					result.VideoStreams.Add(video);
				}
			}

			//string format_details = metadata.attributes.available_format_details[format];
			//var quality;

			return result;

			//				["480p", "720p", "1080p", "2k", "4k"].forEach(function(q)
			//		{
			//			if (format_details.indexOf(q) > -1)
			//				quality = q;
			//		});
			//            var videourl = cdn + cdn_dir + "/v1/" + id + "_" + quality + "_full." + format;
			//		// alternatywa:
			//		var videourlalt;
			//		["480p", "720p", "1080p", "2k", "4k"].forEach(function(q)
			//		{
			//			var k = "video_file_path_" + format + "_" + q;
			//			var a = metadata.items[0].attributes;
			//			if (a.hasOwnProperty(k))
			//				videourlalt = a[k];
			//		});
			//            if(confirm([
			//					 url,
			//					 videourl,
			//					 projection,
			//					 stereo,
			//					 title,
			//					 quality+" ("+format_details.join(", ")+")" 
			//            ].join("\n")))
			//                location.href=videourl;
			//        }
			//};
			//xhr.open("GET", );
			//    xhr.send();
			//})();	
		}
	}

}