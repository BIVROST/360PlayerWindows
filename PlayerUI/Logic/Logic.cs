using BivrostAnalytics;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class Logic
	{
		private static Logic _instance = null;
		public static Logic Instance {
			get {
				if (_instance == null)
					_instance = new Logic();
				return _instance;
			}
		}

		public Settings settings;
		private WebSocketServer webSocketServer;
		public Tracker stats;

		public event Action OnUpdateAvailable = delegate { };

		public Logic()
		{
			Application.Current.DispatcherUnhandledException += (sender, e) =>
			{
				Console.WriteLine("Exception supressed. Success.");
				//throw new NotImplementedException();
			};

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += (sender, e) =>
			{
				Console.WriteLine("Exception supressed. Success.");
				//MessageBox.Show("Exception supressed. Success. ");
            };

			settings = new Settings();
			
			// SETTINGS HACK
			//if(Properties.Settings.Default.InstallId == new Guid())
			//{
				Properties.Settings.Default.InstallId = Guid.NewGuid();
				Properties.Settings.Default.Save();
			//} else
			//{
				Console.WriteLine("InstallId == " + Properties.Settings.Default.InstallId);
			//}

			var OsPlatform = Environment.OSVersion.Platform.ToString();
			var OsVersion = Environment.OSVersion.Version.ToString();
			var OsVersionString = Environment.OSVersion.VersionString;
			var x64 = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			var cpu = Environment.ProcessorCount;

			stats = new Tracker()
			{
				TrackingId = "UA-68212464-1",
				DeviceId = Properties.Settings.Default.InstallId.ToString(),
				UserAgentString = $"BivrostAnalytics/1.0 ({OsPlatform}; {OsVersion}; {OsVersionString}; {x64}; CPU-Cores:{cpu}) {Assembly.GetEntryAssembly().GetName().Name}/{Assembly.GetEntryAssembly().GetName().Version}"
			};

			//==============

			ProtocolHandler.RegisterProtocol();

			Recents.Load();

			Task.Factory.StartNew(() =>
			{
				webSocketServer = new WebSocketServer("ws://127.0.0.1:24876"); // PORT "BIVRO" 24876
				webSocketServer.SupportedSubProtocols = new string[] { "bivrost" };
                try
				{
					webSocketServer.Start(socket =>
					{
						socket.OnOpen = () =>
						{
							Console.WriteLine("Open!");
						};
						socket.OnClose = () => Console.WriteLine("Close!");
						socket.OnMessage = message =>
						{
							switch (message)
							{
								case "version":
									var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
									socket.Send(assembly.Name + ";" + assembly.Version.ToString());
									break;

								default:
									socket.Send(message);
									break;
							};
						};
					});
				}
				catch (Exception) {
					;
				}
			});

		}

		~Logic()
		{
			try
			{
				webSocketServer.Dispose();
			}
			catch (Exception) { }
		}

		public void ReloadPlayer()
		{
			System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
			Application.Current.Shutdown();
		}

		public void CheckForUpdate()
		{
			Task.Factory.StartNew(() =>
			{
				if(Updater.CheckForUpdate())
				{
					OnUpdateAvailable();
				}                
            });
		}

        public void CheckForBrowsers()
        {
            return;

            Task.Factory.StartNew(() =>
            {
                if(!Properties.Settings.Default.browserPluginQuestionShown)
                {
                    var result = MessageBox.Show("Install browser integration extensions?", "Browser addons", MessageBoxButton.YesNo);
                    if(result == MessageBoxResult.Yes)
                    {
                        if(BrowserPluginManagement.CheckFirefox())
                        {
                            BrowserPluginManagement.InstallFirefoxPlugin();
                        }
                        if (BrowserPluginManagement.CheckChrome())
                        {
                            BrowserPluginManagement.InstallChromePlugin();
                        }
                        Properties.Settings.Default.browserPluginQuestionShown = true;
                        Properties.Settings.Default.Save();
                    }
                }
            });
        }
	}
}
