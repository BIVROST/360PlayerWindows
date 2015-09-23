using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Contrib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

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
			string finalUrl2 = HttpUtility.UrlDecode(finalUrl);

			var mapDict = new Dictionary<string, string>();
			unmapped.ToList().ForEach(m => {
				mapDict.Add(m.Split('=')[0], m.Split('=')[1]);
			});

			//finalUrl2 += "&itag=" + mapDict["itag"] + "&signature=" + mapDict["s"] + "&ratebypass=yes";

			return finalUrl2;
		}

		public static string GetVideUrl(string youtubeId)
		{
			var proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "youtube-dl/youtube-dl.exe",
					Arguments = " -f bestvideo -g " + youtubeId,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};

			proc.Start();
			string url = "";
			while (!proc.StandardOutput.EndOfStream)
			{
				url = proc.StandardOutput.ReadToEnd();
			}

			var customUrl = GetVideoUrlFromId(youtubeId);

			Console.WriteLine("youtube-dl: ");
			Console.WriteLine(url);
			Console.WriteLine("custom-dl: ");
			Console.WriteLine(customUrl);

			var youtubedlParameters = url.Split('?')[1].Split('&');
			var customPrameters = customUrl.Split('?')[1].Split('&');
			var youtubedlParametersOrdered = youtubedlParameters.OrderBy(a => a).ToList();
			var customPrametersOrdered = customPrameters.OrderBy(a => a).ToList();

			youtubedlParametersOrdered.ForEach(p => Console.WriteLine(p));
			Console.WriteLine();
			customPrametersOrdered.ForEach(p => Console.WriteLine(p));

			return url;
		}

	}
}
