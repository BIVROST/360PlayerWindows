using Bivrost.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Streaming
{
    public class YoutubeParser : ServiceParser
    {
		private static Logger log = new Logger("YoutubeParser");


        public override string ServiceName
        {
            get { return "YouTube"; }
        }

        public override bool CanParse(string uri)
        {
            if (uri == null)
                return false;
            return
                Regex.IsMatch(uri, @"^(https?://)?(www.youtube.com/watch|youtube.com/watch)/?(.+&|[?])?v=[a-zA-Z0-9_-]+($|&.*|#.*)", RegexOptions.IgnoreCase)
                ||
                Regex.IsMatch(uri, @"^(https?://)?youtu.be/[a-zA-Z0-9_-]+($|&.*|#.*)", RegexOptions.IgnoreCase);
        }


        public override ServiceResult Parse(string uri)
        {
            string dataFoler = Logic.LocalDataDirectory; // Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                                            //if (!Directory.Exists(dataFoler + "\\BivrostPlayer"))
                                                            //	Directory.CreateDirectory(dataFoler + "\\BivrostPlayer");
            string ytdl = dataFoler + "youtube-dl.exe";

            if (!File.Exists(ytdl) || !IsYoutubeUpToDate())
            {
				if (!YoutubeUpdate()) throw new StreamNotSupported("Youtube-dl update failed or declined.");
			}

            int code;
            string jsonStr = YoutubeDL("--ignore-config --no-playlist --no-call-home --dump-json \"" + uri.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"", out code);
            JObject json = JObject.Parse(jsonStr); //  -f 22

            List<AudioStream> audioStreams = new List<AudioStream>();
            List<VideoStream> videoStreams = new List<VideoStream>();

            foreach(JObject format in json["formats"])
            {
                // {
                //  "ext": "webm",
                //  "http_headers": {
                //                    "Accept-Charset": "ISO-8859-1,utf-8;q=0.7,*;q=0.7",
                //    "Accept-Language": "en-us,en;q=0.5",
                //    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                //    "User-Agent": "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20150101 Firefox/47.0 (Chrome)",
                //    "Accept-Encoding": "gzip, deflate"
                //  },
                //  "protocol": "https",
                //  "vcodec": "none",
                //  "format_id": "251",
                //  "format": "251 - audio only (DASH audio)",
                //  "url": "https://r5---sn-f5f7lne7.googlevideo.com/videoplayback?fexp=9405185%2C9405973%2C9419451%2C9422596%2C9428398%2C9431012%2C9433096%2C9433223%2C943C9439362%2C9439580%2C9439596%2C9441651%2C9441746%2C9442424%2C9442426%2C9442496%2C9443322%2C9443353%2C9444451%2C9444731&itag=251&ei=IYOsV7ndMIyBdLTzr9AH&spara2Cmm%2Cmn%2Cms%2Cmv%2Cnh%2Cpl%2Crequiressl%2Csource%2Cupn%2Cexpire&initcwndbps=1373750&expire=1470945153&id=o-AA5pugjseZy23pVbMAnN6odv_aXrkaJLdrRRrEq_SPnD&mnepalive=yes&lmt=1449593038726107&gcr=pl&sver=3&requiressl=yes&upn=pXBlei35JAo&key=yt6&nh=IgpwcjAxLndhdzAyKgkxMjcuMC4wLjE&source=youtube&pl=20&mime=audio%2FweC9EB3C&ratebypass=yes",
                //  "tbr": 149.309,
                //  "format_note": "DASH audio",
                //  "player_url": "//s.ytimg.com/yts/jsbin/player-en_US-vflcnNYP4/base.js",
                //  "acodec": "opus",
                //  "preference": -50,
                //  "abr": 160,
                //  "filesize": 4065132
                //},
                //          {
                //              "ext": "m4a",
                //"http_headers": {
                //                  "Accept-Charset": "ISO-8859-1,utf-8;q=0.7,*;q=0.7",
                //  "Accept-Language": "en-us,en;q=0.5",
                //  "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                //  "User-Agent": "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20150101 Firefox/47.0 (Chrome)",
                //  "Accept-Encoding": "gzip, deflate"
                //},
                //"protocol": "https",
                //"vcodec": "none",
                //"format_id": "140",
                //"format": "140 - audio only (DASH audio)",
                //"url": "https://r5---sn-f5f7lne7.googlevideo.com/videoplayback?fexp=9405185%2C9405973%2C9419451%2C9422596%2C9428398%2C9431012%2C9433096%2C9433223%2C943C9439362%2C9439580%2C9439596%2C9441651%2C9441746%2C9442424%2C9442426%2C9442496%2C9443322%2C9443353%2C9444451%2C9444731&itag=140&ei=IYOsV7ndMIyBdLTzr9AH&spara2Cmm%2Cmn%2Cms%2Cmv%2Cnh%2Cpl%2Crequiressl%2Csource%2Cupn%2Cexpire&initcwndbps=1373750&expire=1470945153&id=o-AA5pugjseZy23pVbMAnN6odv_aXrkaJLdrRRrEq_SPnD&mnepalive=yes&lmt=1458203364425138&gcr=pl&sver=3&requiressl=yes&upn=pXBlei35JAo&key=yt6&nh=IgpwcjAxLndhdzAyKgkxMjcuMC4wLjE&source=youtube&pl=20&mime=audio%2FmpF05DE&ratebypass=yes",
                //"tbr": 128.059,
                //"format_note": "DASH audio",
                //"container": "m4a_dash",
                //"player_url": "//s.ytimg.com/yts/jsbin/player-en_US-vflcnNYP4/base.js",
                //"acodec": "mp4a.40.2",
                //"preference": -50,
                //"abr": 128,
                //"filesize": 3698919
                //      },
                //            {
                //                "ext": "mp4",
                //  "width": 854,
                //  "http_headers": {
                //                    "Accept-Charset": "ISO-8859-1,utf-8;q=0.7,*;q=0.7",
                //    "Accept-Language": "en-us,en;q=0.5",
                //    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                //    "User-Agent": "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20150101 Firefox/47.0 (Chrome)",
                //    "Accept-Encoding": "gzip, deflate"
                //  },
                //  "fps": 30,
                //  "protocol": "https",
                //  "vcodec": "avc1.4d401f",
                //  "format_id": "135",
                //  "format": "135 - 854x480 (480s)",
                //  "url": "https://r5---sn-f5f7lne7.googlevideo.com/videoplayback?fexp=9405185%2C9405973%2C9419451%2C9422596%2C9428398%2C9431012%2C9433096%2C9433223%2C943C9439362%2C9439580%2C9439596%2C9441651%2C9441746%2C9442424%2C9442426%2C9442496%2C9443322%2C9443353%2C9444451%2C9444731&itag=135&ei=IYOsV7ndMIyBdLTzr9AH&spara2Cmm%2Cmn%2Cms%2Cmv%2Cnh%2Cpl%2Crequiressl%2Csource%2Cupn%2Cexpire&initcwndbps=1373750&expire=1470945153&id=o-AA5pugjseZy23pVbMAnN6odv_aXrkaJLdrRRrEq_SPnD&mneepalive=yes&lmt=1458207539575971&gcr=pl&sver=3&requiressl=yes&upn=pXBlei35JAo&key=yt6&nh=IgpwcjAxLndhdzAyKgkxMjcuMC4wLjE&source=youtube&pl=20&mime=video%2Fm635212&ratebypass=yes",
                //  "acodec": "none",
                //  "tbr": 1158.882,
                //  "format_note": "480s",
                //  "player_url": "//s.ytimg.com/yts/jsbin/player-en_US-vflcnNYP4/base.js",
                //  "height": 480,
                //  "preference": -40,
                //  "filesize": 16255553
                //},


                /// v+a
                ///   {
                //   player_url": "//s.ytimg.com/yts/jsbin/player-en_US-vflcnNYP4/base.js",
                //  "format": "22 - 1280x720 (hd720)",
                //  "vcodec": "avc1.64001F",
                //  "height": 720,
                //  "acodec": " mp4a.40.2",
                //  "resolution": "1280x720",
                //  "format_id": "22",
                //  "protocol": "https",
                //  "url": "https://r5---sn-f5f7lne7.googlevideo.com/videoplayback?itag=22&nh=IgpwcjAxLndhdzAyKgkxMjcuMC4wLjE&ipbits=0&pl=20&expire=1470953458&sver=3&dur=232.849&mime=video%2Fmp4&ratebypass=yes&initcwndbps=1380000&gcr=pl&upn=Pj2pgnbl1Zs&id=o-ADp0ozVv3MOODe7frv6Cxk9R8143fN-Qat9dHzfWZSWn&ei=kqOsV9ipKIvOdfS4gtgC&lmt=1458203599323383&ip=84.10.49.238&fexp=9405185%2C9405966%2C9419451%2C9422596%2C9426977%2C9428398%2C9431012%2C9433096%2C9433223%2C9433946%2C9435310%2C9435386%2C9435526%2C9437553%2C9438327%2C9438662%2C9438805%2C9438893%2C9439580%2C9439664%2C9440530%2C9440927%2C9442424%2C9442426%2C9442502%2C9443187%2C9443479%2C9443685%2C9444069%2C9444104&sparams=dur%2Cei%2Cgcr%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cnh%2Cpl%2Cratebypass%2Crequiressl%2Csource%2Cupn%2Cexpire&mv=m&mt=1470931243&ms=au&source=youtube&key=yt6&requiressl=yes&mn=sn-f5f7lne7&mm=31&signature=C9875B8416820E8C2A3C68231DB90C2D4AEB59CC.6E913B115658F923C12F4BC8D1A03FE8268C6059",
                //  "format_note": "hd720",
                //  "ext": "mp4",
                //  "abr": 192,
                //  "http_headers": {
                //                  "Accept-Encoding": "gzip, deflate",
                //    "Accept-Charset": "ISO-8859-1,utf-8;q=0.7,*;q=0.7",
                //    "Accept-Language": "en-us,en;q=0.5",
                //    "User-Agent": "Mozilla/5.0 (X11; Linux x86_64; rv:10.0) Gecko/20150101 Firefox/47.0 (Chrome)",
                //    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
                //  },
                //  "width": 1280
                //}


                /// PARAMS: https://github.com/rg3/youtube-dl#output-template

                /// parsing JSON data
                try
                {
                    int format_id = int.Parse((string)format["format_id"]);
                    int preference = format["preference"] != null ? (int)format["preference"] : 1000000;
                    string ext = (string)format["ext"];
                    string vcodec = (string)format["vcodec"];
                    string acodec = (string)format["acodec"];
                    if (acodec == "none")
                        acodec = null;
                    int? abr = (int?)format["abr"];

                    // is video or video+audio
                    if (vcodec != "none")
                    {
                        int? width = (int?)format["width"];
                        int? height = (int?)format["height"];
                        int? tbr = (int?)format["tbr"];
                        int? vbr = (int?)format["vbr"];

                        VideoContainer vc;
                        switch ((string)format["ext"])
                        {
                            case "mp4":
                                vc = VideoContainer.mp4;
                                break;
                            case "webm":
                                vc = VideoContainer.webm;
                                break;
                            case "3gp":
                                vc = VideoContainer._3gp;
                                break;
                            case "ogv":
                            case "ogg":
                                vc = VideoContainer.ogg;
                                break;
                            default:
                                throw new StreamParsingFailed("Unknown video extension: " + format["ext"]);
                        }

                        // TODO: vcodec
                        //$ youtube - dl.exe https://www.youtube.com/watch?v=edcJ_JNeyhg -s -j | jq -C '.formats[] | .vcodec' | uniq.exe
                        //"none"
                        //"vp9"
                        //"avc1.4d400c"
                        //"avc1.4d4015"
                        //"vp9"
                        //"avc1.4d401e"
                        //"vp9"
                        //"avc1.4d401f"
                        //"vp9"
                        //"avc1.4d401f"
                        //"vp9"
                        //"avc1.640028"
                        //"avc1.640032"
                        //"vp9"
                        //"avc1.640033"
                        //"vp9"
                        //"mp4v.20.3"
                        //"vp8.0"
                        //"avc1.42001E"
                        //"avc1.64001F"

                        // TODO:   acodec
                        //$ youtube - dl.exe https://www.youtube.com/watch?v=edcJ_JNeyhg -s -j | jq -C '.formats[] | .acodec' | uniq.exe
                        //"opus"
                        //"mp4a.40.2"
                        //"vorbis"
                        //"opus"
                        //"none"
                        //" mp4a.40.2"
                        //" vorbis"
                        //" mp4a.40.2"



                        videoStreams.Add(new VideoStream()
                        {
                            audioBitrate = abr,
                            bitrate = vbr.HasValue ? vbr : tbr,
                            container = vc,
                            hasAudio = acodec != null,
                            //audioCodec = AudioCodec.
                            //videoCodec = VideoCodec.
                            width = width,
                            height = height,
                            //quality
                            url = (string)format["url"],
                            quality = format_id // TODO: approx
                        });
                    }
                    // is audio
                    else
                    {
                        if (acodec == null)
                            throw new StreamParsingFailed("stream doesn't contain audio nor video?");
                        audioStreams.Add(new AudioStream()
                        {
                            url = (string)format["url"],
                            quality = format_id // approx
                        });
                    }
                }
                catch(Exception e) when (!(e is StreamException))
                {
					//throw new StreamParsingFailed("Failed to parse youtube-dl downloaded youtube metadata", e);
					log.Error("Failed to parse some of youtube-dl downloaded metadata:\n"+format.ToString()+"\n\n"+e);
                }

            }

            string originalURL = (string)json["webpage_url"];
            return new ServiceResult(originalURL, ServiceName, URIToMediaId(originalURL))
            {
                description = (string)json["description"],
                audioStreams = audioStreams,
                videoStreams = videoStreams,
                stereoscopy = VideoMode.Autodetect,
                title = (string)json["fulltitle"],
                projection = ProjectionMode.Sphere // always?
                // duration = (float)json["duration"]
            };
        }

        // todo: bool updateRequired - run once


        string latestOnlineVersion = null;
        string latestDownloadedVersion = null;
        private bool IsYoutubeUpToDate()
        {
            if (latestOnlineVersion == null)
            {
                RestClient client = new RestClient("http://yt-dl.org/latest/version");
                IRestRequest request = new RestRequest();
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    latestOnlineVersion = response.Content.Trim();
                }
            }

			logger.Info("Latest online version: " + latestOnlineVersion);

            if (!string.IsNullOrWhiteSpace(latestOnlineVersion) && string.IsNullOrEmpty(latestDownloadedVersion))
            {
                int code;
                string version = YoutubeDL("--version", out code);
				if(!string.IsNullOrEmpty(version))
					latestDownloadedVersion = version;
            }

			logger.Info("Latest downloaded version: " + latestDownloadedVersion);

			return latestOnlineVersion == latestDownloadedVersion;
        }


		Logger logger = new Logger("youtube-parser");
        private bool YoutubeUpdate()
        {
			var confirm = System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				var decision = System.Windows.MessageBox.Show(
					System.Windows.Application.Current.MainWindow,
					"A third-party application youtube-dl " +
					"needs to be downloaded from the internet" +
					"and run for this feature to work.\n\n" +
					"Do you want to continue?",

					"Youtube-dl update",
					System.Windows.MessageBoxButton.OKCancel,

					System.Windows.MessageBoxImage.Warning,
					System.Windows.MessageBoxResult.Cancel
				);
				return decision == System.Windows.MessageBoxResult.OK;
			});

			if(!confirm)
			{
				logger.Error("Youtube-dl download declined.");
				return false;
			}
			logger.Info("Youtube-dl download approved.");

			try
			{
                string dataFoler = Logic.LocalDataDirectory;
				// disable SSL3, because github/s3 will break
				System.Net.ServicePointManager.SecurityProtocol &= ~System.Net.SecurityProtocolType.Ssl3;

				RestClient client = new RestClient("https://yt-dl.org/latest/youtube-dl.exe") { FollowRedirects = true, MaxRedirects = 10 };
                IRestRequest request = new RestRequest();
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string ytdl = dataFoler + "youtube-dl.exe";
                    if (File.Exists(ytdl))
                        File.Delete(ytdl);
                    log.Info("youtube-dl update success");
                    File.WriteAllBytes(ytdl, response.RawBytes);
                    latestDownloadedVersion = null;
                    //IsYoutubeUpToDate();
					return true;
                }
                else
                {
                    log.Error($"youtube-dl update failed: {response.StatusCode} {response}");
                }
            }
            catch (Exception exc)
            {
                log.Error(exc, "youtube-dl update failed");
            };

			return false;
        }


        private static string YoutubeDL(string arguments, out int exitCode)
        {
            try
            {
                log.Info("Executing youtube-dl with arguments: " + arguments);
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
                StringBuilder error = new StringBuilder();
                process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);
                process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                exitCode = process.ExitCode;

                log.Info("Youtube-dl exit code: " + exitCode);

                if(!string.IsNullOrWhiteSpace(error.ToString()))
                    log.Error("Youtube-dl errors: " + error.ToString());

                return output.ToString().Trim();

            }
            catch (Exception exc)
            {
                throw new StreamParsingFailed("youtube-dl execution failed", exc);
            }
        }

    }
}
