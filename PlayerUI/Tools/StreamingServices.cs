using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace PlayerUI.Tools
{
	[Obsolete]
	public class StreamingServices
	{
		public enum Service
		{
			Url,
			Youtube,
			Facebook
		}

		public static bool CheckUrlValid(string url, out string correctedUrl, out Uri uriResult)
		{
			correctedUrl = url;
			if(!correctedUrl.ToLower().StartsWith(@"http://") && !correctedUrl.ToLower().StartsWith(@"https://"))
			{
				correctedUrl = @"http://" + url;
			}
			bool result = Uri.TryCreate(correctedUrl, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
			return result;
		}

		public static Service DetectService(Uri uri)
		{
			string domain = string.Join(".", uri.Host.Split('.').Skip(uri.Host.Split('.').Count() - 2));

			switch(domain)
			{
				case "youtube.com": return Service.Youtube;
				case "youtu.be": return Service.Youtube;
				case "facebook.com": return Service.Facebook;
				default: return Service.Url;
			}
		}


		public delegate string ParseFunc(Uri input, out Streaming.ServiceResult serviceResult);


		public static bool TryParseVideoFile(Uri uri, out string fileUrl, out Streaming.ServiceResult serviceResult)
		{
			serviceResult = Streaming.StreamingFactory.Instance.GetStreamingInfo(uri.AbsoluteUri);
			if(serviceResult != null) {
				fileUrl = serviceResult.BestQualityVideoStream(Streaming.VideoContainer.mp4).url;
				return true;
			}

			Service detectedService = DetectService(uri);
			ParseFunc TryParse;

			switch (detectedService)
			{
				case Service.Facebook: TryParse = ParseFacebook; break;
				default: TryParse = ParseUrl; break;
			}

			fileUrl = TryParse(uri, out serviceResult);

			return !string.IsNullOrWhiteSpace(fileUrl);
		}

		private static HtmlDocument DownloadDocument(Uri uri)
		{
			RestClient client = new RestClient(uri);
			IRestRequest request = new RestRequest(Method.GET);
			request.AddHeader("Accept", "text/html");
            IRestResponse response = client.Execute(request);
			string htmlContent = response.Content;
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(htmlContent);
			return document;
		}


		public static string ParseFacebook(Uri uri, out Streaming.ServiceResult serviceResult)
		{
			// HACK
			serviceResult = null;

			try
			{
				//JSON.parse(/"spherical_hd_src":("[^"]+")/.exec(src)[1])
				//https://www.facebook.com/StarWars/videos/1030579940326940/

				RestClient client = new RestClient(uri);
				IRestRequest request = new RestRequest(Method.GET);
				request.AddHeader("Accept", "text/html");
				IRestResponse response = client.Execute(request);
				string htmlContent = response.Content;

				{
					var matches = Regex.Matches(htmlContent, "\"spherical_hd_src\":(\"[^\"]+\")");
					if (matches.Count > 0)
					{
						var g1 = matches[0].Groups[1].Captures[0];
						string videoFile = JsonConvert.DeserializeObject<string>(g1.Value);
						return videoFile;
					}
				}
				{
					var matches = Regex.Matches(htmlContent, "\"spherical_sd_src\":(\"[^\"]+\")");
					if (matches.Count > 0)
					{
						var g1 = matches[0].Groups[1].Captures[0];
						string videoFile = JsonConvert.DeserializeObject<string>(g1.Value);
						return videoFile;
					}
				}
			}
			catch (Exception) { }

			return "";			
		}



		public static string ParseUrl(Uri uri, out Streaming.ServiceResult serviceResult)
		{
			// HACK
			serviceResult = null;

			return uri.ToString();
		}

		//public static string ParseLittlStar(Uri uri, out Streaming.ServiceResult serviceResult)
		//{
		//	// HACK
		//	serviceResult = null;

		//	try
		//	{
		//		HtmlDocument document = DownloadDocument(uri);

		//		var node = document.DocumentNode.Descendants().Where(n => n.Name == "a" && n.GetAttributeValue("class", "").Contains("download")).First().GetAttributeValue("href", "");

		//		return node;
		//	}
		//	catch (Exception) { }

		//	try
		//	{
		//		HtmlDocument document = DownloadDocument(uri);

		//		var node = document.DocumentNode.Descendants()
		//			.Where(n => n.Name == "meta" && n.GetAttributeValue("property", "").Contains("og:video") && n.GetAttributeValue("content","").EndsWith(".mp4"))
		//			.First().GetAttributeValue("content", "");

		//		return node;
		//	}
		//	catch (Exception) { }

		//	return "";
		//}

		public static MediaDecoder.ProjectionMode GetServiceProjection(Uri uri)
		{
			return GetServiceProjection(DetectService(uri));
		}

		public static MediaDecoder.ProjectionMode GetServiceProjection(Service service)
		{
			switch(service)
			{
				case Service.Facebook: return MediaDecoder.ProjectionMode.CubeFacebook;
				default: return MediaDecoder.ProjectionMode.Sphere; break;
			}
		}
	}
}
