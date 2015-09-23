using System;
using System.Collections.Generic;
using System.Web;

namespace Bivrost
{

    public class Protocol
    {
        public enum Stereoscopy
        {
            autodetect, // guess by filename tags and media ratio (see: Media preparation guide)
	        mono, // whole image used
	        side_by_side, // image for left eye is on the left half, and right on the right half of the media
	        top_and_bottom, // the left eye is the top half of the image, the right one in the bottom half
 	        top_and_bottom_reversed // the left eye is the bottom half of the image, the right one in the top half
        }

        public enum Projection {
            equirectangular
        };

        public List<string> urls = new List<string>();
        public Stereoscopy? stereoscopy;
        public Projection? projection;
        public bool? autoplay;
        public bool? loop;
        public string version = null;

        protected Protocol(string link)
        {
            Uri uri = new Uri(link);
            var query=HttpUtility.ParseQueryString(uri.Query);

            if (uri.Scheme != "bivrost")
                throw new ArgumentException("scheme is not bivrost");

            urls.Add(HttpUtility.UrlDecode(uri.LocalPath));
            foreach (string key in query.AllKeys) {
                string val = query.Get(key);
                switch (key) {
                    case "stereoscopy":
                        stereoscopy = (Stereoscopy)Enum.Parse(typeof(Stereoscopy), val.Replace("-", "_"));
                        break;

                    case "projection":
                        projection = (Projection)Enum.Parse(typeof(Projection), val.Replace("-", "_"));
                        break;

                    case "autoplay":
                        autoplay = bool.Parse(val);
                        break;

                    case "loop":
                        loop = bool.Parse(val);
                        break;

                    case "version":
                        version = val;
                        break;

                    case "alt":
                    case "alt0":
                    case "alt1":
                    case "alt2":
                    case "alt3":
                    case "alt4":
                    case "alt5":
                    case "alt6":
                    case "alt7":
                    case "alt8":
                    case "alt9":
                        urls.Add(HttpUtility.UrlDecode(val));
                        break;

                    default:
                        Console.WriteLine("unknown argument in url: "+key+"="+val);
                        break;
                }
            }
        }

        public static Protocol Parse(string link)
        {
            return new Protocol(link);
        }


        public override string ToString()
        {
            return string.Format(
                "[Bivrost.Protocol stereoscopy={0} projection={1} autoplay={2} loop={3} version={4}] urls=",
                stereoscopy,
                projection,
                autoplay,
                loop,
                version
           )+string.Join(", ", urls);
        }
    }

}