using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using RestSharp;
using System.Linq;
using Logger = Bivrost.Log.Logger;

[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("PlayerUI.Test")]

namespace PlayerUI.Streaming
{
	//public enum VideoQuality
	//{
	//	//							width		alias		YT normal		YT spherical
	//	//						s  <1024p			(do not use - filtered out)
	//	unknown = 0,      // ?
	//	hdready = 1280,   //<1920		720p		1280x720			1280x640
	//	fullhd = 1920,		//<2048		1080p		1920x1080		1920x960
	//	q2k = 2048,			//<2560		2048x1024									
	//	q1440p = 2560,		//<3840		1440p		2560x1440		2560x1280
	//	q4k = 4320,			//<7680		2160p		3840x2160		3840x1920
	//	q8k = 7680,       //>=7680		4320p		7680x4320
	//}

	public enum VideoCodec { h264, h265, vp8, vp9, other }
	public enum VideoContainer { mp4, webm, avi, wmv, flv, ogg, _3gp, hls }
	public enum AudioCodec { aac, mp3, opus, other }
	public enum AudioContainer { webm, m4a, mp3, in_video }

	public class VideoStream
	{
		public string url;

		/// <summary>
		/// relative quality - to sort streams by it
		/// </summary>
		public int quality;
		public int? bitrate;

		/// <summary>
		/// guessed frame width, can be null
		/// </summary>
		public int? width;

		/// <summary>
		/// guessed frame height, can be null
		/// </summary>
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


		public override string ToString()
		{
			return string.Format("[VideoStream {0} container={2} has audio={1}, bitrate={3}, size={4}x{5}, q={6}]", url, hasAudio, container, bitrate, width, height, quality);
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
        public int quality;
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
		public List<AudioStream> audioStreams = new List<AudioStream>();

		/// <summary>
		/// All available video streams, some with audio, some without.
		/// Must have at least one.
		/// </summary>
		public List<VideoStream> videoStreams = new List<VideoStream>();

		/// <summary>
		/// Stereoscopy mode (mono, side by side etc)
		/// </summary>
		public MediaDecoder.VideoMode stereoscopy = MediaDecoder.VideoMode.Mono;

		/// <summary>
		/// Projection mode (equirectangular, cubemap types, dome etc)
		/// </summary>
		public MediaDecoder.ProjectionMode projection = MediaDecoder.ProjectionMode.Sphere;

		/// <summary>
		/// Returns highest resolution video stream that is of specific type.
		/// </summary>
		/// <param name="containerType">container that will be required</param>
        /// <param name="mayNotHaveAudio">set to true to make audio optional</param>
		/// <returns>VideoStream or null when not found</returns>
		public VideoStream BestQualityVideoStream(VideoContainer containerType, bool mayNotHaveAudio = false)
		{
            VideoStream best = videoStreams
                .Where(vs => vs.hasAudio || mayNotHaveAudio)
                .Where(vs => vs.container == containerType)
                .OrderByDescending(vs => vs.quality)
                .FirstOrDefault();
            if (best == null)
                throw new NotSupportedException($"No supported video stream with container {containerType} found, audio optional={mayNotHaveAudio}.");
            return best;
		}


		public override string ToString()
		{
			return string.Format("[StreamingResult for {0} ({1}) title={2} ({3} desc.) stereoscopy={4} projection={5} streams a/v={7}/{6}]",
					originalURL, serviceName, title, description?.Length, stereoscopy, projection, videoStreams.Count, audioStreams.Count);
		}

	}


	public class StreamingFactory {

		protected StreamingFactory() { }

		private static StreamingFactory instance;
		public static StreamingFactory Instance { 
			get {
					if (instance == null)
						instance = new StreamingFactory();
					return instance;
			} 
		}

		protected ServiceParser[] parsers = new ServiceParser[] {
			new VrideoParser(),
			new PornhubParser(),
			new LittlstarParser(),
            new YoutubeParser(),
            new URLParser()
		};

		/// <summary>
		/// Returns a fully parsed streaming service result with audio and video url and metadata,
		/// </summary>
		/// <param name="uri">url of the service</param>
		/// <returns>Streaming service result or null when this url is not supported</returns>
		/// <exception>Throws a StreamingNotSupported, StreamNetworkFailure or StreamParsingFailed on errors</exception>
		public ServiceResult GetStreamingInfo(string uri)
		{
			foreach(var parser in parsers)
				if(parser.CanParse(uri)) {
					return parser.Parse(uri);
				}
			return null;
		}


	}


	public abstract class ServiceParser {

		#region util

		static HttpClient client;
		internal static async Task<string> HTTPGetStringAsync(string uri) {
			try {
				if (client == null)
					client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };
				var response = await client.GetAsync(uri);
				if (!response.IsSuccessStatusCode)
					throw new StreamNetworkFailure("Status " + response.StatusCode, uri);
				return await response.Content.ReadAsStringAsync(); ;
			}
			catch (HttpRequestException e) {
				throw new StreamNetworkFailure("HttpRequestException " + e.Message, uri);
			}
		}

		internal static string HTTPGetString(string uri, bool defaultHttp=true)
		{
			if (!uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) && !uri.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
				uri = (defaultHttp ? "http://" : "https://") + uri;
			RestClient client = new RestClient(uri);
			IRestRequest request = new RestRequest(Method.GET);
			request.AddHeader("Accept", "text/html");
			IRestResponse response = client.Execute(request);

			Logger.Info("HTTP request: " + uri);

			if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 400)
				throw new StreamNetworkFailure("Status "+response.StatusCode, uri);

			if (response.ErrorException != null)
				throw new StreamNetworkFailure(response.ErrorMessage, uri, response.ErrorException); 
			return response.Content;
		}

		#endregion

		/// <summary>
		/// Checks if the uri can be parsed by this class.
		/// If this returns true, no other classes are involved.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>true if this class understands this uri</returns>
		public abstract bool CanParse(string uri);

		/// <summary>
		/// Tries to parse an url and get audio and video information from it, specific for each service
		/// </summary>
		/// <param name="url">url of the service</param>
		/// <returns>true if succeeded</returns>
		public abstract ServiceResult Parse(string uri);
		
	}


	public abstract class StreamException : Exception {
		public StreamException(string message, Exception innerException = null) : base(message, innerException) { }
	}

	/// <summary>
	/// Thrown when the stream is not understood. API change, possible bug, etc.
	/// </summary>
	[Serializable]
	public class StreamParsingFailed : StreamException
	{
		public StreamParsingFailed(string message, Exception innerException=null) : base(message, innerException) { }
	}

	/// <summary>
	/// Thrown when the stream is understood, but the player does not support it.
	/// </summary>
	[Serializable]
	public class StreamNotSupported : StreamException
	{
		public StreamNotSupported(string reason, Exception innerException = null) : base(reason, innerException) { }
	}

	/// <summary>
	/// Thrown on network errors
	/// </summary>
	[Serializable]
	public class StreamNetworkFailure : StreamException
	{
		public string Uri { get; protected set;  }
		public StreamNetworkFailure(string reason, string uri, Exception innerException = null) : base(reason, innerException) {
			Uri = uri;
		}
	}



}
