using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Streaming
{

    public class URLParser : ServiceParser
    {
        public override string ServiceName
        {
            get { return "plain URL"; }
        }

        public override bool CanParse(string url)
        {
            var uri = new Uri(url);
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        public override ServiceResult Parse(string uri)
        {
			// TODO: check media type

			// TODO: if html, try to parse it?
			// TODO: search for bivrost-360webplayer tags

			return new ServiceResult(uri, ServiceName, URIToMediaId(uri))
            {
                projection = MediaDecoder.ProjectionMode.Sphere,
				stereoscopy = GuessStereoscopyFromFileName(uri),
                videoStreams = new List<VideoStream>()
                {
                    new VideoStream()
                    {
                        url = uri,
                        hasAudio = true, // TODO
                        container = GuessContainerFromExtension(uri)
                    }
                }
            };
        }
    }
}
