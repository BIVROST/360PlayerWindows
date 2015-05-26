using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Contrib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlayerUI
{
	class YoutubeEngine
	{
		public static string GetVideoUrlFromId(string youtubeId)
		{
			RestClient client = new RestClient("https://youtube.com/");
			IRestRequest request = new RestRequest("watch", Method.GET);
			request.AddParameter("v", youtubeId);
			IRestResponse response = client.Execute(request);
			string htmlContent = response.Content;

			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(htmlContent);
			var node = document.DocumentNode.Descendants().Where(n => n.Name == "script" && n.InnerHtml.Contains("ytplayer.config")).First();
			Match match = Regex.Match(node.InnerHtml, @"(?<=ytplayer.config = )(.*)(?=;ytplayer.load)");
			string found = match.Value;
			dynamic d = JObject.Parse(found);
			string magicValue = d.args.adaptive_fmts;
			string unmaagicValue = HttpUtility.HtmlDecode(magicValue);
			var maps = unmaagicValue.Split(',').ToList();
			string map = maps[0];
			var unmapped = map.Split('&');
			string urlLine = unmapped.Where(line => line.Contains("url=")).First();
			string finalUrl = HttpUtility.UrlDecode(urlLine.Split('=')[1]);

			return finalUrl;
		}

	}
}
