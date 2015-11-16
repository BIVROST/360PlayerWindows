using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
    public class BrowserPluginManagement
    {
        public static string chromePath = "";
        public static string firefoxPath = "";

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

            RegistryKey myApps = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths", true);
            if (myApps.GetSubKeyNames().Contains(appName))
            {
                RegistryKey chromeKey = myApps.OpenSubKey(appName);
                appPath = chromeKey.GetValue("").ToString();
                appFound = true;
            }
            myApps.Close();

            RegistryKey localApps = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
            if (localApps.GetSubKeyNames().Contains(appName))
            {
                RegistryKey chromeKey = localApps.OpenSubKey(appName);
                appPath = chromeKey.GetValue("").ToString();
                appFound = true;
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
            string tempFile = Path.GetTempFileName();
            string ffPlugin = @"https://addons.mozilla.org/firefox/downloads/latest/249334/addon-249334-latest.xpi?src=hp-dl-featured";
            using (var writer = File.OpenWrite(tempFile))
            {
                var client = new RestSharp.RestClient(ffPlugin);
                var request = new RestSharp.RestRequest("");
                request.ResponseWriter = (responseStream) => responseStream.CopyTo(writer);
                var response = client.Execute(request);
                writer.Flush();
                writer.Close();
                var profilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox\\Profiles";

                if (Directory.Exists(profilesDir))
                {
                    foreach(string profile in Directory.GetDirectories(profilesDir))
                    {
                        if(profile.EndsWith(".default"))
                        {
                            var destCopy = profile + "\\extensions\\" + response.ResponseUri.Segments.Last();
                            Console.WriteLine("Copy to: " + destCopy);
                            File.Copy(tempFile, destCopy);
                        }
                    }
                }
                File.Delete(tempFile);
            }
            
        }

        public static void InstallChromePlugin()
        {
            
        }
    }
}
