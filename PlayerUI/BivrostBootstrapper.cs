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
			SetAddRemoveProgramsIcon();
			AssociateFileExtensions();
			//DeviceSelectionTool.EnumerateGraphicCards();
			string[] args = Environment.GetCommandLineArgs();
			try {
				if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
				{
					string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
					if(activationData != null)
						if(activationData.Length > 0)
							ShellViewModel.FileFromArgs = activationData[0];
				}
			} catch(Exception ex)
			{
				//MessageBox.Show("ex: " + ex.Message + "\n" + ex.StackTrace);
			}

			if (mutex.WaitOne(TimeSpan.Zero, true))
			{
				ownMutex = true;

				if (args.Length > 1)
					if (!string.IsNullOrWhiteSpace(args[1]))
					{
						ShellViewModel.FileFromArgs = args[1];
					}

				if (Logic.Instance.settings.EventMode)
					DisplayRootViewFor<EventShellViewModel>();
				else
					DisplayRootViewFor<ShellViewModel>();				
			}
			else
			{
				

				if (args.Length > 1)
					if (!string.IsNullOrWhiteSpace(args[1]))
					{
						Clipboard.SetText(args[1]);
						NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);
					}
				Application.Shutdown();
			}
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			base.OnExit(sender, e);
			if(ownMutex)
				mutex.ReleaseMutex();
		}

		private static void SetAddRemoveProgramsIcon()
		{
			//only run if deployed 
			if (File.Exists("updated")) return;
			File.WriteAllText("updated", "");

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
				RegistryKey bivrostProtocolKey = myClasses.OpenSubKey("bivrost");
				if (bivrostProtocolKey == null)
					myClasses.CreateSubKey(@"bivrost\shell\open\command");
				bivrostProtocolKey = myClasses.OpenSubKey("bivrost", true);
				bivrostProtocolKey.SetValue("URL Protocol","");
				RegistryKey commandKey = bivrostProtocolKey.OpenSubKey(@"shell\open\command", true);
				commandKey.SetValue("", "\"" + apppath + "\"" + " %1");


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
