using BivrostAnalytics;
using Fleck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public enum HeadsetMode
	{
		Auto,
		OSVR,
		Oculus,
		[Description("SteamVR (OpenVR, HTC Vive)")]
		OpenVR,
		Disable
	}

	public enum ScreenSelection
	{
		One,
		Two,
		Three
	}

	public class Logic
	{
		public static string LocalDataDirectory = "";// Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BivrostPlayer";

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
            };			
			settings = new Settings();

			ConfigureSettingsActions();

			if (settings.InstallId == Guid.Empty)
			{
				settings.InstallId = Guid.NewGuid();
				settings.Save();
			}
			else
			{
				Console.WriteLine("InstallId == " + settings.InstallId);
			}

			var OsPlatform = Environment.OSVersion.Platform.ToString();
			var OsVersion = Environment.OSVersion.Version.ToString();
			var OsVersionString = Environment.OSVersion.VersionString;
			var x64 = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			var cpu = Environment.ProcessorCount;

			stats = new Tracker()
			{
				TrackingId = "UA-68212464-1",
				DeviceId = settings.InstallId.ToString(),
				UserAgentString = $"BivrostAnalytics/1.0 ({OsPlatform}; {OsVersion}; {OsVersionString}; {x64}; CPU-Cores:{cpu}) {Assembly.GetEntryAssembly().GetName().Name}/{Assembly.GetEntryAssembly().GetName().Version}"
			};

			//==============

			ProtocolHandler.RegisterProtocol();

			Recents.Load();

			Task.Factory.StartNew(() =>
			{
				webSocketServer = new WebSocketServer("ws://127.0.0.1:24876"); // PORT "BIVRO" 24876
				//var cert = Certificate.CreateSelfSignCertificatePfx(null, DateTime.Now, DateTime.Now.AddYears(2));
				//var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(cert);
				//webSocketServer.Certificate = x509;
				//webSocketServer.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Default;
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
									string Version = "";
									try
									{
										if (Tools.PublishInfo.ApplicationIdentity != null)
											Version = Tools.PublishInfo.ApplicationIdentity.Version.ToString();
									}
									catch (Exception) { }
									if (string.IsNullOrWhiteSpace(Version))
									{
										if (Assembly.GetExecutingAssembly() != null)
											Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
										else
											Version = "( not supported )";
									}


									socket.Send(assembly.Name + ";" + Version);
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

		private void ConfigureSettingsActions()
		{
			settings.InstallPlugins = () =>
			{
				settings.BrowserPluginQuestionShown = false;
				CheckForBrowsers();
			};

			settings.ResetInstallId = () =>
			{
				var result = System.Windows.Forms.MessageBox.Show("Do you really want to reset installation ID?", "Installation ID", System.Windows.Forms.MessageBoxButtons.YesNo);
				if(result ==  System.Windows.Forms.DialogResult.Yes)
				{
					settings.InstallId = Guid.NewGuid();
					settings.Save();
					System.Windows.Forms.Application.Restart();
					System.Windows.Application.Current.Shutdown();
				}
			};

			settings.ResetConfiguration = () =>
			{
				var result = System.Windows.Forms.MessageBox.Show("Reset configuration to default?", "Configuration", System.Windows.Forms.MessageBoxButtons.YesNo);
				if (result ==  System.Windows.Forms.DialogResult.Yes)
				{
					try
					{
						System.IO.File.Delete(settings.SettingsFile);
						System.Windows.Forms.Application.Restart();
						System.Windows.Application.Current.Shutdown();
					}
					catch (Exception exc) { }					
				}
			};
		}

		public void CheckForBrowsers()
		{
			if (string.IsNullOrWhiteSpace(Logic.LocalDataDirectory)) return;

			if (!settings.BrowserPluginQuestionShown)
			{
				

				Task.Factory.StartNew(() =>
				{
					var result = System.Windows.Forms.MessageBox.Show("Install browser integration extensions?", "Browser addons", System.Windows.Forms.MessageBoxButtons.YesNoCancel);

					if (result == System.Windows.Forms.DialogResult.Yes)
					{
					
						try {
							if (BrowserPluginManagement.CheckFirefox())
							{
								BrowserPluginManagement.InstallFirefoxPlugin();
							}
						}
						catch (Exception exc) { }

						try
						{
							if (BrowserPluginManagement.CheckChrome())
							{
								BrowserPluginManagement.InstallChromePlugin();
							}
						}
						catch (Exception exc) {  }

						settings.BrowserPluginAccepted = true;
						settings.BrowserPluginQuestionShown = true;
						
						settings.Save();
						
					}
					else if (result == System.Windows.Forms.DialogResult.No)
					{
						settings.BrowserPluginQuestionShown = true;
						settings.Save();
					}
				});
			}
			else
			{
				Task.Factory.StartNew(() =>
				{
					if (settings.BrowserPluginAccepted)
					{
						if (BrowserPluginManagement.CheckFirefox())
						{
							BrowserPluginManagement.InstallFirefoxPlugin();
						}
						if (BrowserPluginManagement.CheckChrome())
						{
							BrowserPluginManagement.InstallChromePlugin();
						}
					}
				});
			}				
		}

        internal void ValidateSettings()
        {
            if (!Directory.Exists(Logic.Instance.settings.RemoteControlMovieDirectory)){
                Logic.Instance.settings.RemoteControlMovieDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                Logic.Instance.settings.Save();
            }
        }
    }
}
