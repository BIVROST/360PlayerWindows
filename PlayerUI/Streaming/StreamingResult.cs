using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using RestSharp;

namespace PlayerUI.Streaming
{
	public enum VideoQuality
	{
		//							width		alias		YT normal		YT spherical
		//						s  <1024p			(do not use - filtered out)
		unknown = 0,      // ?
		hdready = 1280,   //<1920		720p		1280x720			1280x640
		fullhd = 1920,		//<2048		1080p		1920x1080		1920x960
		q2k = 2048,			//<2560		2048x1024									
		q1440p = 2560,		//<3840		1440p		2560x1440		2560x1280
		q4k = 4320,			//<7680		2160p		3840x2160		3840x1920
		q8k = 7680,       //>=7680		4320p		7680x4320
	}

	public enum VideoCodec { h264, h265, vp8, vp9, other }
	public enum VideoContainer { mp4, webm, avi, wmv, flv, ogg, _3gp }
	public enum AudioCodec { aac, mp3, opus, other }
	public enum AudioContainer { webm, m4a, mp3, in_video }

	public class VideoStream
	{
		public string url;
		public VideoQuality quality;
		public int? bitrate;
		public int? width;
		public int? height;
		public long? size;

		public bool hasAudio;
		public VideoContainer container;
		public VideoCodec? videoCodec;



		/// TODO: move audio info to AudioStream object
		public AudioCodec? audioCodec;
		public int? audioBitrate;
		public int? audioFrequency;

		public AudioStream AsAudio
		{
			get
			{
				if (hasAudio == false)
					return null;
				return new AudioStream()
				{
					bitrate = audioBitrate,
					size = null,
					codec = audioCodec,
					container = AudioContainer.in_video,
					url = null,
					frequency = audioFrequency
				};
			}
		}

	}


	public class AudioStream
	{
		public string url;
		public int? bitrate;
		public long? size;
		public AudioCodec? codec;
		public int? frequency;
		public AudioContainer? container;
	}



	public class ServiceResult
	{
		/// <summary>
		/// The URL where the movie can be viewed as intended by the website its from.
		/// </summary>
		public string originalURL;

		/// <summary>
		/// The displayable name of the service, ex. YouTube
		/// </summary>
		public string serviceName;

		/// <summary>
		/// The title of this movie, if available. Can be null.
		/// </summary>
		public string title = null;

		/// <summary>
		/// The description of this movie, if available. Can be null.
		/// </summary>
		public string description = null;

		/// <summary>
		/// All available stand alone audio streams, excluding those that are in video files.
		/// Often empty.
		/// </summary>
		public List<AudioStream> AudioStreams = new List<AudioStream>();

		/// <summary>
		/// All available video streams, some with audio, some without.
		/// Must have at least one.
		/// </summary>
		public List<VideoStream> VideoStreams = new List<VideoStream>();

		/// <summary>
		/// Stereoscopy mode (mono, side by side etc)
		/// </summary>
		public MediaDecoder.VideoMode stereoscopy = MediaDecoder.VideoMode.Mono;

		/// <summary>
		/// Projection mode (equirectangular, cubemap types, dome etc)
		/// </summary>
		public MediaDecoder.ProjectionMode projection = MediaDecoder.ProjectionMode.Sphere;

		//public VideoStream BestQualityVideoStream {
		//	get {
		//		return VideoStreams.ToList().Aggregate((agg, next) => next.quality > agg.quality ? next : agg);
		//	}
		//}

	}


	public class StreamingFactory {


		protected ServiceParser[] parsers = new ServiceParser[] {
			new VrideoParser()
		};

		/// <summary>
		/// Returns a fully parsed streaming service result with audio and video url and metadata,
		/// </summary>
		/// <param name="uri">url of the service</param>
		/// <returns>Streaming service result or null on failure</returns>
		public async Task<ServiceResult> GetStreamingInfo(string uri)
		{
			foreach(var parser in parsers)
				if(parser.CanParse(uri)) {
					//ServiceResult result=await parser.TryParse(uri);
					//return result;
				}
			return null;
		}


	}


	public abstract class ServiceParser {

		#region util

		protected void Warn(string message) {
			Console.WriteLine("[" + GetType().Name + " warning]: " + message);
		}

		HttpClient client;
		public async Task<string> HTTPGetStringAsync(string uri) {
			if(client == null)
				client= new HttpClient() { MaxResponseContentBufferSize = 1000000 };
			var response = await client.GetStringAsync(uri);
			return response;
		}

		protected string HTTPGetString(string uri)
		{
			RestClient client = new RestClient(uri);
			IRestRequest request = new RestRequest(Method.GET);
			// request.AddHeader("Accept", "text/html");
			IRestResponse response = client.Execute(request);
			return response.Content;
		}

		#endregion


		public abstract bool CanParse(string uri);

		/// <summary>
		/// Tries to parse an url and get audio and video information from it, specific for each service
		/// </summary>
		/// <param name="url">url of the service</param>
		/// <returns>true if succeeded</returns>
		public abstract ServiceResult TryParse(string uri);
		
	}


	[Serializable]
	internal class StreamParsingFailed : Exception
	{
		public StreamParsingFailed(string message) : base(message)
		{
		}
	}

	[Serializable]
	public class StreamNotSupported : Exception
	{
		public StreamNotSupported(string reason) : base(reason)
		{
		}
	}
}
