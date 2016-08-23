using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Streaming
{

    public class URLParser : ServiceParser
    {
        public override bool CanParse(string url)
        {
            var uri = new Uri(url);
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        public override ServiceResult Parse(string uri)
        {
            // TODO: parse containers (uri.LocalPath.EndsWith())

            // TODO: check media type

            // TODO: if html, try to parse it?

            // TODO: search for bivrost-360webplayer tags
            
            // TODO: parse tags from filenames (SbS, TaB etc)

            //RestClient client = new RestClient(uri);
            //IRestRequest request = new RestRequest(Method.HEAD);
            //request.AddHeader("Accept", "text/html");
            //IRestResponse response = client.Execute(request);
            //if (response.StatusCode != System.Net.HttpStatusCode.OK)
            //{
            //    if (view != null)
            //        Execute.OnUIThreadAsync(() =>
            //        {
            //            Valid = false;
            //            TryClose();
            //        });
            //    return;

            //}


            return new ServiceResult()
            {
                originalURL = uri,
                projection = MediaDecoder.ProjectionMode.Sphere,
                serviceName = "plain URL",
                videoStreams = new List<VideoStream>()
                {
                    new VideoStream()
                    {
                        url = uri,
                        container = VideoContainer.mp4  // TODO
                    }
                }
            };
        }
    }
}
