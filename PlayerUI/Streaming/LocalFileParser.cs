using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Streaming
{

	public class LocalFileParser : ServiceParser
	{
		public override bool CanParse(string url)
		{
			try
			{
				// this will throw an ArgumentException if path is incorrect
				string urlFull = Path.GetFullPath(url);

				// if url is not rooted, it is invalid
				return Path.IsPathRooted(url);
			}
			catch (ArgumentException) { return false; }
			catch (NotSupportedException) { return false; }
		}

		public override ServiceResult Parse(string uri)
		{
			string path = Path.GetFullPath(uri);
			if (!File.Exists(path))
				throw new StreamNetworkFailure("File not found", path);

			return new ServiceResult()
			{
				originalURL = path,
				projection = MediaDecoder.ProjectionMode.Sphere,
				stereoscopy = GuessStereoscopyFromFileName(uri),
				serviceName = "Local file",
				videoStreams = new List<VideoStream>()
				{
					new VideoStream()
					{
						url = uri,
						hasAudio = true, // TODO
                        container = GuessContainerFromExtension(path)
                    }
				}
			};
		}
	}
}
