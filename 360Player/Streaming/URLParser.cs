﻿using System;
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

			var container = GuessContainerFromExtension(uri);
			return new ServiceResult(uri, ServiceName, URIToMediaId(uri))
            {
                projection = ProjectionMode.Sphere,
				stereoscopy = GuessStereoscopyFromFileName(uri),
                videoStreams = new List<VideoStream>()
                {
                    new VideoStream()
                    {
                        url = uri,
                        hasAudio = true, // TODO, currently guessed
                        container = container
                    }
                },
				contentType = GuessContentTypeFromContainer(container),
				title = Uri.UnescapeDataString(Path.GetFileName(uri))
			};
        }
    }
}
