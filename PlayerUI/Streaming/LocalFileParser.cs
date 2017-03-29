using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

            FileInfo fileInfo = new FileInfo(path);

            int consideredBytes = 1048576;
            if (fileInfo.Length < consideredBytes)
                consideredBytes = (int)fileInfo.Length;

            byte[] data = new byte[consideredBytes];
            fileInfo.OpenRead().Read(data, 0, consideredBytes);

            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] hash = sha.ComputeHash(data);
            string mediaId = $"sha1+len:{string.Concat(Array.ConvertAll(hash, x => x.ToString("x2")))}+{fileInfo.Length}";

            return new ServiceResult(path, ServiceName, mediaId)
			{
				projection = MediaDecoder.ProjectionMode.Sphere,
				stereoscopy = GuessStereoscopyFromFileName(uri),
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

        public override string ServiceName
        {
            get { return "Local file"; }
        }
    }
}
