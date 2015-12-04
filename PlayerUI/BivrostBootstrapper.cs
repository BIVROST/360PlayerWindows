using Caliburn.Micro;
using Microsoft.Win32;
using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class BivrostBootstrapper : BootstrapperBase
	{
		// APP GUID 150A3700-F729-4FE2-B61B-C3C716530C58
		// Single instance lock
		static Mutex mutex = new Mutex(true, "{41F397A7-D196-4C6F-B75A-616069D45DAD}");
		bool ownMutex = false;

		public BivrostBootstrapper()
		{
			Initialize();
		}

		protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
		{
			System.Windows.Forms.Application.EnableVisualStyles();

			if (ApplicationDeployment.IsNetworkDeployed)
			{
				Logic.LocalDataDirectory = ApplicationDeployment.CurrentDeployment.DataDirectory + "\\";

				SetAddRemoveProgramsIcon();
				AssociateFileExtensions();
			} else
			{
				{
					string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
					Logic.LocalDataDirectory = Path.GetDirectoryName(path) + "\\";
				}

			}


			
			

			string[] args = Environment.GetCommandLineArgs();
			
			try {
				if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
				{
					string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;

					if (activationData != null)
					{
						if (activationData.Length == 1)
						{
							ShellViewModel.FileFromArgs = activationData[0];
						}
						if(activationData.Length == 2)
						{
							if (activationData[0] == "--bivrost-protocol")
							{
								ShellViewModel.FileFromProtocol = activationData[1];
                            }
                        }
					}							
				}
				
			} catch(Exception ex)
			{
				//MessageBox.Show("ex: " + ex.Message + "\n" + ex.StackTrace);
			}


			try
			{
				mutex.WaitOne(TimeSpan.Zero, true);
			}
			catch(AbandonedMutexException exc)
			{
				mutex.ReleaseMutex();
			}

			if (mutex.WaitOne(TimeSpan.Zero, true))
			{
				ownMutex = true;

				if (args.Length == 2)
				{
					if (!string.IsNullOrWhiteSpace(args[1]))
					{
						ShellViewModel.FileFromArgs = args[1];
					}
				} else
				if(args.Length == 3)
				{
					if (!string.IsNullOrWhiteSpace(args[1]) && !string.IsNullOrWhiteSpace(args[2]))
					{
						if (args[1] == "--bivrost-protocol")
							ShellViewModel.FileFromProtocol = args[2];
					}
                }
				
				if (Logic.Instance.settings.EventMode)
				{
					DisplayRootViewFor<EventShellViewModel>();
				}
				else
				{
					DisplayRootViewFor<ShellViewModel>();
				}
				
			}
			else
			{
				if (args.Length == 2)
				{
					if (!string.IsNullOrWhiteSpace(args[1]))
					{
						Clipboard.SetText(args[1]);
						NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);
					}
				} else if(args.Length == 3)
				{
					if (!string.IsNullOrWhiteSpace(args[1]) && !string.IsNullOrWhiteSpace(args[2]))
					{
						if (args[1] == "--bivrost-protocol")
						{
							Clipboard.SetText(args[2]);
							NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);
						}
					}
				}
				Application.Shutdown();
			}
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(Logic.LocalDataDirectory))
				{
					foreach (string s in Directory.EnumerateFiles(Logic.LocalDataDirectory))
					{
						//MessageBox.Show(s);
						File.Copy(s, Path.GetFileName(s), true);
					}
				}
			}
			catch (Exception exc) { }

			base.OnExit(sender, e);
			if(ownMutex)
				mutex.ReleaseMutex();
		}

		private static void SetAddRemoveProgramsIcon()
		{
			//only run if deployed 
			try {
				if (File.Exists(System.Windows.Forms.Application.StartupPath + "updated")) return;
				File.WriteAllText(System.Windows.Forms.Application.StartupPath + "updated", "");
			}
			catch (Exception e)
			{
				
			}

			if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
			{
				try
				{
					Assembly code = Assembly.GetExecutingAssembly();
					AssemblyDescriptionAttribute asdescription =
						(AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(code, typeof(AssemblyDescriptionAttribute));
					string assemblyDescription = asdescription.Description;

					//the icon is included in this program
					string iconSourcePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Graphics\\fileassoc.ico");

					if (!File.Exists(iconSourcePath))
						return;

					RegistryKey myUninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
					string[] mySubKeyNames = myUninstallKey.GetSubKeyNames();
					for (int i = 0; i < mySubKeyNames.Length; i++)
					{
						RegistryKey myKey = myUninstallKey.OpenSubKey(mySubKeyNames[i], true);
						object myValue = myKey.GetValue("DisplayName");
						if (myValue != null && myValue.ToString().ToLower().Contains("bivrost"))
						{
                            myKey.SetValue("DisplayIcon", iconSourcePath);
							break;
						}
					}
				}
				catch (Exception ex)
				{
					//log an error
				}
			}
		}

		public static void AssociateFileExtensions()
		{
			try {
				
				string publisherName = GetPublisher("Bivrost 360Player");
				string apppath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), publisherName, "Bivrost 360Player.appref-ms");
				RegistryKey myClasses = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
				if(myClasses == null) myClasses = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes", true);
				RegistryKey bivrostProtocolKey = myClasses.OpenSubKey("bivrost");
				if (bivrostProtocolKey == null)
					myClasses.CreateSubKey(@"bivrost\shell\open\command");
				bivrostProtocolKey = myClasses.OpenSubKey("bivrost", true);
				bivrostProtocolKey.SetValue("URL Protocol","");
				RegistryKey commandKey = bivrostProtocolKey.OpenSubKey(@"shell\open\command", true);
				commandKey.SetValue("","\"" + System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Substring(8).Replace('/',Path.DirectorySeparatorChar) + "\" --bivrost-protocol \"%1\"");
			} catch(Exception exc) { }
        }

		public static string GetPublisher(string application)
		{
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
			{
				var appKey = key.GetSubKeyNames().FirstOrDefault(x => GetValue(key, x, "DisplayName") == application);
				if (appKey == null) { return null; }
				return GetValue(key, appKey, "Publisher");
			}
		}

		private static string GetValue(RegistryKey key, string app, string value)
		{
			using (var subKey = key.OpenSubKey(app))
			{
				if (!subKey.GetValueNames().Contains(value)) { return null; }
				return subKey.GetValue(value).ToString();
			}
		}


	}
}
