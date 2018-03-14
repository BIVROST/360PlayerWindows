using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
    public static class BrowserPluginManagement
    {
        public static string chromePath = "";
        public static string firefoxPath = "";

		public static void CheckForBrowsers()
		{
			var settings = Logic.Instance.settings;

			if (string.IsNullOrWhiteSpace(Logic.LocalDataDirectory)) return;

			if (!settings.BrowserPluginQuestionShown)
			{


				Task.Factory.StartNew(() =>
				{
					var result = System.Windows.Forms.MessageBox.Show("Install browser integration extensions?", "Browser addons", System.Windows.Forms.MessageBoxButtons.YesNoCancel);

					if (result == System.Windows.Forms.DialogResult.Yes)
					{

						try
						{
							if (CheckFirefox())
							{
								InstallFirefoxPlugin();
							}
						}
						catch (Exception exc) { }

						try
						{
							if (CheckChrome())
							{
								InstallChromePlugin();
							}
						}
						catch (Exception exc) { }

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
						if (CheckFirefox())
						{
							InstallFirefoxPlugin();
						}
						if (CheckChrome())
						{
							InstallChromePlugin();
						}
					}
				});
			}
		}

		public static bool CheckFirefox()
        {
            return CheckForApp("firefox.exe", out firefoxPath);
        }

        public static bool CheckChrome()
        {
            return CheckForApp("chrome.exe", out chromePath);
        }

        private static bool CheckForApp(string appName, out string appPath)
        {
            bool appFound = false;
            appPath = "";

            RegistryKey myApps = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
			if(myApps == null) myApps = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
			if (myApps != null)
			{
				if (myApps.GetSubKeyNames().Contains(appName))
				{
					RegistryKey chromeKey = myApps.OpenSubKey(appName);
					appPath = chromeKey.GetValue("").ToString();
					appFound = true;
				}
				myApps.Close();
			}

            RegistryKey localApps = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
			if(localApps == null) localApps = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
			if (localApps != null)
			{
				if (localApps.GetSubKeyNames().Contains(appName))
				{
					RegistryKey chromeKey = localApps.OpenSubKey(appName);
					appPath = chromeKey.GetValue("").ToString();
					appFound = true;
				}
				localApps.Close();
			}

			return appFound;
		}

        public static void RunBrowser(string appPath)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            //proc.UseShellExecute = true;
            //proc.WorkingDirectory = ;
            proc.FileName = appPath;

            Process.Start(proc);
        }

        public static void InstallFirefoxPlugin()
        {
			try
			{
				//string ffPlugin = @"https://download.bivrost360.com/ff-plugin/bivrost_360player_connector-1.0-fx-windows.xpi";
				string ffPlugin = @"https://download.bivrost360.com/ff-plugin/firefox-connector-extension@bivrost360.com.xpi";

				var client = new RestSharp.RestClient(ffPlugin);
				var request = new RestSharp.RestRequest("");
				var response = client.Execute(request);
				if (response.StatusCode == System.Net.HttpStatusCode.OK)
				{
					string pluginDir = Logic.LocalDataDirectory + "ffplugin";

					if (Directory.Exists(pluginDir))
						Directory.Delete(pluginDir, true);

					if (!Directory.Exists(pluginDir))
						Directory.CreateDirectory(pluginDir);

					var destCopy = pluginDir + "\\" + response.ResponseUri.Segments.Last();
					File.WriteAllBytes(destCopy, response.RawBytes);
					ZipFile.ExtractToDirectory(destCopy, pluginDir);
					File.Delete(destCopy);

					string extensionID = "firefox-connector-extension@bivrost360.com";

					// 32
					{
						RegistryKey chromeKey = Registry.CurrentUser.OpenSubKey(@"Software\Mozilla\Firefox\", true);
						if (chromeKey != null)
						{
							RegistryKey extensionsKey = chromeKey.OpenSubKey(@"Extensions", true);
							if (extensionsKey == null)
							{
								extensionsKey = chromeKey.CreateSubKey("Extensions");
							}
							extensionsKey.SetValue(extensionID, pluginDir);
							
							extensionsKey.Close();
							chromeKey.Close();
						}
					}
					

					//var profilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox\\Profiles";

						//if (Directory.Exists(profilesDir))
						//{
						//	foreach (string profile in Directory.GetDirectories(profilesDir))
						//	{
						//		if (profile.EndsWith(".default"))
						//		{
						//			var destCopy = profile + "\\extensions\\" + response.ResponseUri.Segments.Last();
						//			Console.WriteLine("Copy to: " + destCopy);
						//			//File.Copy(tempFile, destCopy, true);
						//			File.WriteAllBytes(destCopy, response.RawBytes);
						//		}
						//	}
						//}
				}
			}
			catch (Exception exc) { }
        }


        public static void InstallChromePlugin()
        {
			//32 - bit Windows: HKEY_CURRENT_USER\Software\Google\Chrome\Extensions\< id > \update_url = https://clients2.google.com/service/update2/crx
			//64 - bit Windows: HKEY_CURRENT_USER\Software\Wow6432Node\Google\Chrome\Extensions\< id > \update_url = https://clients2.google.com/service/update2/crx

            try
			{
				// 32 bit
				{
					RegistryKey chromeKey = Registry.CurrentUser.OpenSubKey(@"Software\Google\Chrome\", true);
					if(chromeKey == null)
						chromeKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Google\Chrome\", true);
					if (chromeKey != null)
					{
						RegistryKey extensionsKey = chromeKey.OpenSubKey(@"Extensions", true);
						if (extensionsKey == null)
						{
							extensionsKey = chromeKey.CreateSubKey("Extensions");
						}
						RegistryKey pluginKey = extensionsKey.CreateSubKey("goloffhdkngdkldehjjijobinpfcjpbj");
						pluginKey.SetValue("update_url", "https://clients2.google.com/service/update2/crx");

						pluginKey.Close();
						extensionsKey.Close();
						chromeKey.Close();
					}
				}
				//// 64 bit
				//{
				//	RegistryKey bitKey = Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node", true);
				//	if(bitKey != null)
				//	{
				//		RegistryKey chromeKey = Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Google\Chrome\", true);
				//		if (chromeKey != null)
				//		{
				//			RegistryKey extensionsKey = chromeKey.OpenSubKey(@"Extensions", true);
				//			if (extensionsKey == null)
				//			{
				//				extensionsKey = chromeKey.CreateSubKey("Extensions");
				//			}
				//			RegistryKey pluginKey = extensionsKey.CreateSubKey("goloffhdkngdkldehjjijobinpfcjpbj");
				//			pluginKey.SetValue("update_url", "https://clients2.google.com/service/update2/crx");

				//			pluginKey.Close();
				//			extensionsKey.Close();
				//			chromeKey.Close();
				//		}
				//	}
				//}

			} catch(Exception exc)
			{

			}
        }
    }
}
