//using Nancy;
using System;
//using Nancy.Hosting.Self;
using System.Diagnostics;
//using Nancy.ModelBinding;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using System.Globalization;
using System.Collections.Concurrent;

/// <summary>
/// Wymaga pakietów z NuGet:
///  - Nancy
///  - Nancy.Hosting.Self
///  - JSON.net
/// </summary>
namespace Bivrost.Bivrost360Player
{
    public class ApiServer
		: NancyModule
    {

		public static NancyHost InitNancy(Action<ApiServer> onInit, int port = 8080)
		{
			OnInit += onInit;

			System.Threading.Thread.CurrentThread.CurrentCulture =
			System.Threading.Thread.CurrentThread.CurrentUICulture =
			//System.Globalization.CultureInfo.DefaultThreadCurrentCulture=
			System.Globalization.CultureInfo.InvariantCulture;

			string filePath = Process.GetCurrentProcess().MainModule.FileName;
			//Process.Start("netsh", "advfirewall firewall delete rule name=\"Remote Video Player\"");
			//Process.Start("netsh", "advfirewall firewall delete rule name=\"Open Port " + port + "\"");
			//Process.Start("netsh", "advfirewall firewall add rule name=\"Remote Video Player\" dir=in action=allow program=\"" + filePath + "\" enable=yes");
			//Process.Start("netsh", "advfirewall firewall add rule name=\"Open Port " + port + "\" dir=in action=allow protocol=TCP localport=" + port);

			var host = new NancyHost(
				new System.Uri("http://localhost:" + port),
				new DefaultNancyBootstrapper(),
				new HostConfiguration()
				{
					UrlReservations = new UrlReservations() { CreateAutomatically = true }
				}
			);
			host.Start();

			return host;
		}


		static string Log(params object[] p)
		{
			System.Console.Write("[API] ");
			var str = string.Join(" ", p);
			System.Console.WriteLine(str);
			return str;
		}


		class MovieContainer { public string movie; }

		/// <summary>
		/// w Unity3d jest przerobiona kopia
		/// </summary>
		public class Status
		{
			public Dictionary<int, string[]> commands = new Dictionary<int, string[]>();

			public int max_id = 0;

			internal void Ack(int maxId)
			{
				foreach (var k in commands.Keys.ToList())
					if (k <= maxId)
						commands.Remove(k);
			}

			internal void Command(params string[] p)
			{
				commands[++max_id] = p;
			}

			internal string ToJson()
			{
				return JsonConvert.SerializeObject(this);
			}
		}


		#region API

		/// <summary>
		/// W unity3d jest kopia
		/// </summary>
		public enum State
		{
			init, stop, play, pause, off
		};

		public static Status status = new Status();
		public static string device_id;
		public static string[] movies;
		public static float t01;
		public static Tuple<float, float, float> euler = new Tuple<float, float, float>(0, 0, 0);
		public static State state;

		public static void CommandPlay(string path) { status.Command("play", path); }
		public static void CommandPause() { status.Command("pause"); }
		public static void CommandUnPause() { status.Command("unpause"); }
		public static void CommandStop() { status.Command("stop"); }
		public static void CommandReset() { status.Command("reset"); }
		public static void CommandSettings(bool repeat, bool resetOnPutOn) { status.Command("settings", repeat ? "true" : "false", resetOnPutOn ? "true" : "false"); }
		public static void CommandMessage(string message) { status.Command("message", message); }
		public static void CommandOffset(float eulerX, float eulerY, float eulerZ) { status.Command("offset", eulerX.ToString("000.0"), eulerY.ToString("000.0"), eulerZ.ToString("000.0")); }
		public static void CommandSeek01(float t01) { status.Command("seek01", t01.ToString("0.000000")); }
		public static void CommandSeek(float t) { status.Command("seek01", t.ToString("00000.00")); }
		public static void CommandReinit() { status.Command("reinit"); }


        // Control API handlers
        public static Func<string[]> CommandMoviesHandler = null;
        public static Func<string, bool, bool> CommandLoadHandler = null;
        public static Func<float, bool> CommandSeekHandler = null;
        public static Func<bool> CommandStopAndResetHandler = null;
        public static Func<bool> CommandPauseHandler = null;
        public static Func<bool> CommandUnpauseHandler = null;
        public static Func<PlayingInfo> CommandPlayingHandler = null;
        

        public static event Action<ApiServer> OnInit;
		public static event Action<State> OnStateChange;
		public static event Action<Tuple<float, float, float>, float> OnPos;
		public static event Action<Tuple<float, float, float, float>, float> OnPosQuaternion;
		public static event Action OnBackPressed;
		public static event Action<string> OnInfo;
		public static event Action<string> OnConfirmPlay;

        #endregion

        /// <summary>
        /// Wywoływane automatycznie przez Nancy
        /// </summary>
        public ApiServer() : base("/v1/")
        {
            Get["/"] = _ => "api v1";

            Post["/init"] = p =>
            {
                status = new Status();
                device_id = null;
                t01 = 0;
                euler = new Tuple<float, float, float>(0, 0, 0);
                state = State.init;

                var q = Request.Query;
                device_id = q.device_id;
                movies = this.Bind<List<MovieContainer>>().ConvertAll(mc => mc.movie).ToArray();

                if (OnInit != null)
                    OnInit(this);

                return status.ToJson();
            };

            Get["/state-change"] = p =>
            {
                var q = Request.Query;
                state = System.Enum.Parse(typeof(State), q.state);

                if (OnStateChange != null)
                    OnStateChange(state);

                return status.ToJson();
            };

            Get["/pos"] = p =>
            {
                var q = Request.Query;
                euler = new Tuple<float, float, float>(float.Parse(q.euler_x, CultureInfo.InvariantCulture), float.Parse(q.euler_y, CultureInfo.InvariantCulture), float.Parse(q.euler_z, CultureInfo.InvariantCulture));
                t01 = float.Parse(q.t01, CultureInfo.InvariantCulture);
                var quat = new Tuple<float, float, float, float>(
                    float.Parse(q.quat_x, CultureInfo.InvariantCulture),
                    float.Parse(q.quat_y, CultureInfo.InvariantCulture),
                    float.Parse(q.quat_z, CultureInfo.InvariantCulture),
                    float.Parse(q.quat_w, CultureInfo.InvariantCulture)
                    );

                if (OnPos != null)
                    OnPos(euler, t01);

                if (OnPosQuaternion != null)
                    OnPosQuaternion(quat, t01);

                return status.ToJson();
            };

            Get["/back"] = _ =>
            {
                if (OnBackPressed != null)
                    OnBackPressed();

                return status.ToJson();
            };

            Get["/ack"] = p =>
            {
                var q = Request.Query;
                int max_id = q.max_id;
                status.Ack(max_id);

                return status.ToJson();
            };

            Post["/info"] = p =>
            {
                string message = Request.Form["message"];

                if (OnInfo != null)
                    OnInfo(message);

                return status.ToJson();
            };

            Post["/confirm-play"] = p =>
            {
                string path = Request.Form["path"];

                if (OnConfirmPlay != null)
                    OnConfirmPlay(path);

                return status.ToJson();
            };

            /// control API

            /// TODO
            //Get["/movies"] = p => JsonConvert.SerializeObject(new string[] { "TODO", "list", "movies" });
            Get["/movies"] = p => JsonConvert.SerializeObject(CommandMoviesHandler == null ? new string[] { } : CommandMoviesHandler());

            //Get["/load"] = p => { bool autoplay = p.autoplay; string movie = p.movie; return "true"; };
            Get["/load"] = p =>
            {
                var q = Request.Query;
                return JsonConvert.SerializeObject(CommandLoadHandler == null ? false : CommandLoadHandler(q.movie, q.autoplay));
            };

            //Get["/seek"] = p => { float t = p.t; return "true"; };
            Get["/seek"] = p => JsonConvert.SerializeObject(CommandSeekHandler == null ? false : CommandSeekHandler(Request.Query.t));

            //Get["/stop-and-reset"] = p => "true";
            Get["/stop-and-reset"] = p => JsonConvert.SerializeObject(CommandStopAndResetHandler == null ? false : CommandStopAndResetHandler());

            //Get["/pause"] = p => "true";
            Get["/pause"] = p => JsonConvert.SerializeObject(CommandPauseHandler == null ? false : CommandPauseHandler());

            //Get["/unpause"] = p => "true";
            Get["/unpause"] = p => JsonConvert.SerializeObject(CommandUnpauseHandler == null ? false : CommandUnpauseHandler());

            //       Get["/playing"] = p => JsonConvert.SerializeObject(new PlayingInfo() 
            //{ is_playing = true, movie="fake", 
            //  quat_x=0.1f, quat_y=0.4f, quat_z=0.2f, quat_w=(float)Math.Sqrt(0.1*0.1 + 0.4*0.4 + 0.2+0.2), 
            //  t=13.37f, tmax=66.60f	}
            Get["/playing"] = p => JsonConvert.SerializeObject(CommandPlayingHandler == null ? new PlayingInfo() { is_playing = false, movie = "", quat_x = 0f, quat_y = 0f, quat_z = 0f, quat_w = 0f, t = 0f, tmax = 0f } : CommandPlayingHandler());
			
            
            /// END TODO

			Get["/events"] = p =>
			{
				OutgoingEvent ev;
				// timeout after 10 seconds and return an empty array
				if (outgoingEvents.TryTake(out ev, TimeSpan.FromSeconds(10)))
				{
					var r = new List<OutgoingEvent> { ev };
					while (outgoingEvents.TryTake(out ev, TimeSpan.FromMilliseconds(0.1)))
						r.Add(ev);
					// ignore all events older than 5 sec
					r.RemoveAll(e => e.created.Elapsed > TimeSpan.FromMilliseconds(5000));
					return JsonConvert.SerializeObject(r);
				}

				return "[]";
			};

			// temp
			Get["/__eventtester"] = p =>
			{
				AddOutgoingEvent("ev1", "arg1");
				AddOutgoingEvent("ev2", "arg2");
				AddOutgoingEvent("ev3", "arg3");
				return "\"done\"";
			};
		}

		static BlockingCollection<OutgoingEvent> outgoingEvents = new BlockingCollection<OutgoingEvent>();

		class OutgoingEvent {
			public string name;
			public string arg;

			[JsonIgnore]
			public Stopwatch created=new Stopwatch();
		}

		public static void AddOutgoingEvent(string name, string arg) {
			var og = new OutgoingEvent() { name = name, arg = arg };
			og.created.Start();
			outgoingEvents.Add(og);
		}





		public struct PlayingInfo
		{

			/// <summary>
			/// The path to the current movie.
			/// </summary>
			public string movie;

			/// <summary>
			/// The current camera orientation quaternion
			/// </summary>
			public float quat_x;

			/// <summary>
			/// The current camera orientation quaternion
			/// </summary>
			public float quat_y;

			/// <summary>
			/// The current camera orientation quaternion
			/// </summary>
			public float quat_z;

			/// <summary>
			/// The current camera orientation quaternion
			/// </summary>
			public float quat_w;


			/// <summary>
			/// Current playback time
			/// </summary>
			public float t;


			/// <summary>
			/// Movie length
			/// </summary>
			public float tmax;


			/// <summary>
			/// Is the movie is currently playing
			/// </summary>
			public bool is_playing;

		}

	}
}
