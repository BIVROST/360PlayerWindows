using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlayerUI.Streaming
{
    public class FacebookParser : ServiceParser
    {
        public override bool CanParse(string uri)
        {
            return false;
        }

        public override ServiceResult Parse(string uri)
        {
            throw new NotImplementedException();

            //ServiceResult sr = new ServiceResult()
            //{
            //    originalURL = uri,
            //    videoStreams = new List<VideoStream>(),
            //    projection = MediaDecoder.ProjectionMode.CubeFacebook
            //};


            //string htmlContent = HTTPGetString(uri);
            //{
            //    var matches = Regex.Matches(htmlContent, "\"spherical_hd_src\":(\"[^\"]+\")");
            //    if (matches.Count > 0)
            //    {
            //        var g1 = matches[0].Groups[1].Captures[0];
            //        sr.videoStreams.Add(new VideoStream()
            //        {
            //            url = JsonConvert.DeserializeObject<string>(g1.Value),
            //            quality = 2
            //        });
            //    }
            //}
            //{
            //    var matches = Regex.Matches(htmlContent, "\"spherical_sd_src\":(\"[^\"]+\")");
            //    if (matches.Count > 0)
            //    {
            //        var g1 = matches[0].Groups[1].Captures[0];
            //        sr.videoStreams.Add(new VideoStream()
            //        {
            //            url = JsonConvert.DeserializeObject<string>(g1.Value),
            //            quality = 1
            //        });
            //    }
            //}

            //return sr;
        }
    }
}
