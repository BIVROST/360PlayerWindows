﻿using HtmlAgilityPack;
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
	public class StreamingServices
	{
		public enum Service
		{
			Url,
			Youtube,
			Facebook,
			Vrideo,
			LittlStar,
			Pornhub
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
				case "vrideo.com": return Service.Vrideo;
				case "littlstar.com": return Service.LittlStar;
				case "pornhub.com": return Service.Pornhub;
				default: return Service.Url;
			}
		}

		public static bool TryParseVideoFile(Uri uri, out string fileUrl)
		{
			Service detectedService = DetectService(uri);
			Func<Uri, string> TryParse;

			switch (detectedService)
			{
				case Service.Facebook: TryParse = ParseFacebook; break;
				case Service.Youtube: TryParse = ParseYoutube; break;
				case Service.LittlStar: TryParse = ParseLittlStar; break;
				case Service.Vrideo: TryParse = ParseVrideo; break;
				case Service.Pornhub: TryParse = ParsePornhub; break;
				default: TryParse = ParseUrl; break;
			}

			fileUrl = TryParse(uri);

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


		public static string ParseFacebook(Uri uri)
		{
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

		public static string ParseYoutube(Uri uri)
		{
            try
            {
				#region experimental code
				/*
                HtmlDocument document = DownloadDocument(uri);
                var html = document.DocumentNode.InnerHtml;

                var match = Regex.Match(html, "(?<=ytplayer.config = )(.*)(?=;ytplayer.load)");
                var g = match.Groups[1].Value;

                var json = JsonConvert.DeserializeObject(g);

                Newtonsoft.Json.Linq.JValue a = (Newtonsoft.Json.Linq.JValue)(json as Newtonsoft.Json.Linq.JObject)["args"]["url_encoded_fmt_stream_map"];
                string fmt = a.Value.ToString();
                string[] fmtTab = fmt.Split(',');

                string hdline = fmtTab.Where(l => l.Contains("quality=hd720"))?.First();
                string[] hdconfig = hdline.Split('&');
                Dictionary<string, string> config = hdconfig.ToDictionary(t => t.Split('=')[0], t => t.Split('=')[1]);
                string url = HttpUtility.UrlDecode(config["url"]);
                //if (config.ContainsKey("s"))
                //    url += "&signature=" + config["s"];

                Uri u = new Uri(url, UriKind.Absolute);
                System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(u.Query);
                string sparams = HttpUtility.UrlDecode(query["sparams"]);

                sparams.Split(',').ToList().ForEach(sp =>
                {
                    Console.WriteLine("PARAM: " + sp + " => " + (query.AllKeys.Contains(sp) ? "OK" : "--"));
                });

                var node = document.DocumentNode.ChildNodes["html"].ChildNodes["head"].ChildNodes.First(n => {
                    return n.Attributes.Any(attr => attr.Name == "name" && attr.Value == "player/base");
                });
                string src = node.Attributes["src"].Value;
                string script;

                {
                    //RestClient client = new RestClient("https:" + src);
                    RestClient client = new RestClient("http://bivrost360.com/js-test.js");
                    IRestRequest request = new RestRequest();                    
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        script = response.Content;

                        Jint.Engine jint = new Jint.Engine();
                        jint.Execute(script);


                        var value = jint.Invoke("cr", config["s"]);
                        //int it = 1;
                        //scriptLines.ToList().ForEach(sl =>
                        //{
                        //    try {
                        //        jint.Execute(sl);
                        //    } catch(Exception jintExc)
                        //    {
                        //        Console.WriteLine("[JINT EXC] " + jintExc.Message);
                        //    }
                        //    it++;
                        //});
                        url += "&signature=" + value.ToString();
                        ;
                    }
                }
                */
				#endregion

				string dataFoler = Logic.LocalDataDirectory; // Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				//if (!Directory.Exists(dataFoler + "\\BivrostPlayer"))
				//	Directory.CreateDirectory(dataFoler + "\\BivrostPlayer");
				string ytdl = dataFoler + "youtube-dl.exe";

				if (!File.Exists(ytdl))
				{
					YoutubeUpdate();	
				} else if(!IsYoutubeUpToDate())
				{
					YoutubeUpdate();
				}

				int code;
				string url = YoutubeDL("-f 22 -g " + uri.AbsoluteUri, out code);
				return url;
            }
            catch (Exception exc) {
                Console.WriteLine("[EXC] " + exc.Message);
                return "";
            }
		}


		private static bool IsYoutubeUpToDate()
		{
			string latestVersion = null;
			RestClient client = new RestClient("http://yt-dl.org/latest/version");
			IRestRequest request = new RestRequest();
			IRestResponse response = client.Execute(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				latestVersion = response.Content.Trim();
			}

			if(!string.IsNullOrWhiteSpace(latestVersion))
			{
				int code;
				string version = YoutubeDL("--version", out code);
				if (version == latestVersion) return true;
			}

			return false;
		}

		private static void YoutubeUpdate()
		{
			try {
				string dataFoler = Logic.LocalDataDirectory; //Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				//if (!Directory.Exists(dataFoler + "\\BivrostPlayer"))
				//	Directory.CreateDirectory(dataFoler + "\\BivrostPlayer");
				string ytdl = dataFoler + "youtube-dl.exe";

				if (File.Exists(ytdl))
				{
					File.Delete(ytdl);
				}

				RestClient client = new RestClient("https://yt-dl.org/latest/youtube-dl.exe");
				IRestRequest request = new RestRequest();
				IRestResponse response = client.Execute(request);
				if (response.StatusCode == System.Net.HttpStatusCode.OK)
				{
					File.WriteAllBytes(ytdl, response.RawBytes);
				}
			} catch (Exception exc) {
				return;
			};
		}

		private static string YoutubeDL(string arguments, out int exitCode)
		{
			try
			{		
				string dataFoler = Logic.LocalDataDirectory; //Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				//if (!Directory.Exists(dataFoler + "\\BivrostPlayer"))
				//	Directory.CreateDirectory(dataFoler + "\\BivrostPlayer");
				string ytdl = dataFoler + "youtube-dl.exe";

				Process process = new Process();
				ProcessStartInfo start = new ProcessStartInfo(ytdl);
				start.Arguments = arguments;
				start.CreateNoWindow = true;
				start.RedirectStandardOutput = true;
				start.RedirectStandardError = true;
				start.UseShellExecute = false;

				process.StartInfo = start;
				StringBuilder output = new StringBuilder();
				process.OutputDataReceived += (sender, e) => { output.AppendLine(e.Data); };
				process.ErrorDataReceived += (sender, e) => { output.AppendLine(e.Data); };

				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();
				exitCode = process.ExitCode;

				return output.ToString().Trim();

			} catch(Exception exc)
			{
				exitCode = -1;
				return "";
			}
        }



		public static string ParseVrideo(Uri uri)
		{
			try {
				{
					string video4k = @"http://cdn2.vrideo.com/prod_videos/v1/" + uri.Segments.Last() + "_4k_full.mp4";
					RestClient client = new RestClient(video4k);
					IRestRequest request = new RestRequest(Method.HEAD);
					request.AddHeader("Accept", "text/html");
					IRestResponse response = client.Execute(request);
					if (response.StatusCode == System.Net.HttpStatusCode.OK)
						return video4k;
				}

				{
					string video2k = @"http://cdn2.vrideo.com/prod_videos/v1/" + uri.Segments.Last() + "_2k_full.mp4";
					RestClient client = new RestClient(video2k);
					IRestRequest request = new RestRequest(Method.HEAD);
					request.AddHeader("Accept", "text/html");
					IRestResponse response = client.Execute(request);
					if (response.StatusCode == System.Net.HttpStatusCode.OK)
						return video2k;
				}
				{
					string video1080p = @"http://cdn2.vrideo.com/prod_videos/v1/" + uri.Segments.Last() + "_1080p_full.mp4";
					RestClient client = new RestClient(video1080p);
					IRestRequest request = new RestRequest(Method.HEAD);
					request.AddHeader("Accept", "text/html");
					IRestResponse response = client.Execute(request);
					if (response.StatusCode == System.Net.HttpStatusCode.OK)
						return video1080p;
				}
			} catch(Exception) { }
			return "";
			//return @"http://cdn2.vrideo.com/prod_videos/v1/"+uri.Segments.Last()+"_4k_full.mp4";
		}

		public static string ParsePornhub(Uri uri)
		{
			//pornhub /player_quality_720p\s*=\s*'([^']+)'/.exec(document.body.innerHTML)[1]

			HtmlDocument document = DownloadDocument(uri);
			try
			{
				{
					var match = Regex.Match(document.DocumentNode.InnerHtml, @"player_quality_1080p\s*=\s*'([^']+)'");

					if (match.Captures.Count > 0)
					{
						return match.Groups[1].Captures[0].Value;
					}
				}

				{
					var match = Regex.Match(document.DocumentNode.InnerHtml, @"player_quality_720p\s*=\s*'([^']+)'");

					if (match.Captures.Count > 0)
					{
						return match.Groups[1].Captures[0].Value;
					}
				}

				{
					var match = Regex.Match(document.DocumentNode.InnerHtml, @"player_quality_480p\s*=\s*'([^']+)'");

					if (match.Captures.Count > 0)
					{
						return match.Groups[1].Captures[0].Value;
					}
				}

				{
					var match = Regex.Match(document.DocumentNode.InnerHtml, @"player_quality_240p\s*=\s*'([^']+)'");

					if (match.Captures.Count > 0)
					{
						return match.Groups[1].Captures[0].Value;
					}
				}

				{
					var match = Regex.Match(document.DocumentNode.InnerHtml, @"player_quality_180p\s*=\s*'([^']+)'");

					if (match.Captures.Count > 0)
					{
						return match.Groups[1].Captures[0].Value;
					}
				}

			}
			catch (Exception) { }
			return "";
		}

		public static string ParseUrl(Uri uri)
		{
			return uri.ToString();
		}

		public static string ParseLittlStar(Uri uri)
		{
			try
			{
				HtmlDocument document = DownloadDocument(uri);

				var node = document.DocumentNode.Descendants().Where(n => n.Name == "a" && n.GetAttributeValue("class", "").Contains("download")).First().GetAttributeValue("href", "");

				return node;
			}
			catch (Exception) { }

			try
			{
				HtmlDocument document = DownloadDocument(uri);

				var node = document.DocumentNode.Descendants()
					.Where(n => n.Name == "meta" && n.GetAttributeValue("property", "").Contains("og:video") && n.GetAttributeValue("content","").EndsWith(".mp4"))
					.First().GetAttributeValue("content", "");

				return node;
			}
			catch (Exception) { }

			return "";
		}

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