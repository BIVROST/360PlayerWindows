using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RestSharp;
using System.Text.RegularExpressions;
using RestSharp.Contrib;
using Newtonsoft.Json.Linq;

namespace YoutubeDownloader
{
	class Program
	{
		static void Main(string[] args)
		{
			//string youtube = @"https://www.youtube.com/watch?v=7IaYJZ2Usdk";

			RestClient client = new RestClient("https://youtube.com/");
			IRestRequest request = new RestRequest("watch", Method.GET);
			request.AddParameter("v", "7IaYJZ2Usdk");
			IRestResponse response = client.Execute(request);
			string htmlContent = response.Content;

			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(htmlContent);
			var node = document.DocumentNode.Descendants().Where(n => n.Name == "script" && n.InnerHtml.Contains("ytplayer.config")).First();
			Match match = Regex.Match(node.InnerHtml, @"(?<=ytplayer.config = )(.*)(?=;ytplayer.load)");
			string found = match.Value;
			dynamic d = JObject.Parse(found);
			string magicValue = d.args.url_encoded_fmt_stream_map;
			string unmaagicValue = HttpUtility.HtmlDecode(magicValue);
			var maps = unmaagicValue.Split(',').ToList();
			string map = maps[0];
			var unmapped = map.Split('&');
			string urlLine = unmapped.Where(line => line.Contains("url=")).First();
			string finalUrl = HttpUtility.UrlDecode(urlLine.Split('=')[1]);
			Console.ReadLine();
		}
	}
}
