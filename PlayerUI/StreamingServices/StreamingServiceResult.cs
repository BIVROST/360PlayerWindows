using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.StreamingServices
{
	public enum VideoQuality
	{
							//width		alias		normal			spherical
							//<720p			(do not use - filtered out)
		unknown=0,		// ?
		hdready=1280,	//<1080		720p		1280x720			1280x640
		fullhd=1920,	//<1440		1080p		1920x1080		1920x960
		q1440p=2560,	//<2160		1440p		2560x1440		2560x1280
		q4k=4320,		//<4320		2160p		3840x2160		3840x1920
		q8k=7680			//>=4320		4320p		7680x4320
	}

	public enum VideoCodec { h264, h265, vp8, vp9, other }
	public enum VideoContainer { mp4, webm, avi, wmv, flv, _3gp }
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
		public bool? hasAudio;
		public VideoContainer container;
		public VideoCodec? videoCodec;


		/// TODO: move audio info to AudioStream object
		public AudioCodec? audioCodec;
		public AudioContainer? audioContainer;
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
					container = audioContainer,
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



	abstract class ServiceResult
	{
		/// <summary>
		/// The URL where the movie can be viewed as intended by the website its from.
		/// </summary>
		public string OriginalURL { get; }

		/// <summary>
		/// The displayable name of the service, ex. YouTube
		/// </summary>
		public string ServiceName { get; }

		/// <summary>
		/// The title of this movie, if available. Can be null.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// The description of this movie, if available. Can be null.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// All available stand alone audio streams, excluding those that are in video files.
		/// Often empty.
		/// </summary>
		public AudioStream[] AudioStreams { get; }

		/// <summary>
		/// All available video streams, some with audio, some without.
		/// Must have at least one.
		/// </summary>
		public VideoStream[] VideoStreams { get; }

		//public VideoStream BestQualityVideoStream {
		//	get {
		//		return VideoStreams.ToList().Aggregate((agg, next) => next.quality > agg.quality ? next : agg);
		//	}
		//}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static async Task<ServiceResult> Parse(string url) {
			return null;
		}

	}




}
