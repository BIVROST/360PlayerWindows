using Caliburn.Micro;
using Microsoft.Win32;
using PlayerUI.Tools;
using System;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bivrost.Log;

namespace PlayerUI
{
	public class BivrostBootstrapper : BootstrapperBase
	{
		// APP GUID 150A3700-F729-4FE2-B61B-C3C716530C58
		// Single instance lock
		static Mutex mutex = new Mutex(true, "{41F397A7-D196-4C6F-B75A-616069D45DAD}");
		bool ownMutex = false;
		private static Logger logger;

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

		public BivrostBootstrapper()
		{
			Initialize();
		}



		protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
		{

			//AddPathPatch();

			logger = new Logger("bootstrap");

			Application.Current.DispatcherUnhandledException += (_s, _e) => logger.Fatal(_e.Exception, "unhandled application exception");
			AppDomain.CurrentDomain.UnhandledException += (_s, _e) => logger.Fatal(_e.ExceptionObject as Exception, "unhandled application domain exception");

			Logic.Prepare();

			LoggerManager.RegisterListener(new TextFileLogListener(Logic.LocalDataDirectory));
			LoggerManager.RegisterListener(new TraceLogListener(false));

			System.Windows.Forms.Application.EnableVisualStyles();

			if (!System.Diagnostics.Debugger.IsAttached && ApplicationDeployment.IsNetworkDeployed)
			{
				Logic.LocalDataDirectory = ApplicationDeployment.CurrentDeployment.DataDirectory + "\\";

				Task.Factory.StartNew(() =>
				{
					CopyApplicationReference();
					SetAddRemoveProgramsIcon();
					AssociateFileExtensions();
				});
			}
			else
			{
				{
                    string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase, true)).AbsolutePath;
                    //string path = Assembly.GetExecutingAssembly().CodeBase;
                    Logic.LocalDataDirectory = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
				}
			}


			string[] args = Environment.GetCommandLineArgs();
			
			try {
				if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
				{
					args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
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

			if (mutex.WaitOne(TimeSpan.Zero, true) || System.Diagnostics.Debugger.IsAttached)
			{
				ownMutex = true;

				if(args != null && args.Length > 0)
				{
					if(!args[args.Length - 1].EndsWith(".exe"))
						ShellViewModel.FileFromArgs = args[args.Length - 1];
				}
				
				DisplayRootViewFor<ShellViewModel>();
				
			}
			else
			{
				if (args.Length > 0)
				{
					if (!string.IsNullOrWhiteSpace(args[args.Length - 1]))
					{
						string str = args[args.Length - 1];
						var cds = new NativeMethods.COPYDATASTRUCT
						{
							dwData = new IntPtr(3),
							cbData = str.Length + 1,
							lpData = str
						};

						//MessageBox.Show("Sending: " + cds.lpData);

						IntPtr bwin = GetPlayerWindow();
						NativeMethods.SendMessage(bwin, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref cds);


						//Clipboard.SetText(args[1]);
						//NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);

						//string str = args[0];
						//var cds = new NativeMethods.COPYDATASTRUCT
						//{
						//	dwData = new IntPtr(3),
						//	cbData = str.Length + 1,
						//	lpData = str
						//};
						//IntPtr bwin = NativeMethods.FindWindow(null, "Bivrost 360Player");
						//NativeMethods.SendMessage(bwin, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref cds);

					}
				} 

				//	else if(args.Length == 3)
				//{
				//	if (!string.IsNullOrWhiteSpace(args[1]) && !string.IsNullOrWhiteSpace(args[2]))
				//	{
				//		if (args[1] == "--bivrost-protocol")
				//		{
				//			Clipboard.SetText(args[2]);
				//			NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);
				//		}
				//	}
				//}
				Application.Shutdown();
			}
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			//try
			//{
			//	if (!string.IsNullOrWhiteSpace(Logic.LocalDataDirectory))
			//	{
			//		foreach (string s in Directory.EnumerateFiles(Logic.LocalDataDirectory))
			//		{
			//			//MessageBox.Show(s);
			//			File.Copy(s, Path.GetFileName(s), true);
			//		}
			//	}
			//}
			//catch (Exception exc) { }

			base.OnExit(sender, e);
			try {
				if (ownMutex)
					mutex.ReleaseMutex();
			} catch(Exception exc)
			{
				System.Diagnostics.Debug.WriteLine("Releasing mutex: " + exc.Message);
			}
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

		public static void CopyApplicationReference()
		{
			try
			{
				// http://download.bivrost360.com/player-desktop/canary/BivrostPlayer.application#BivrostPlayer.application, Culture=en, PublicKeyToken=8b49056c26c8df4d, processorArchitecture=msil
				if(!File.Exists(Logic.LocalDataDirectory + "Bivrost360Player.appref-ms"))
				{
					string appref = $"http://download.bivrost360.com/player-desktop/canary/BivrostPlayer.application#BivrostPlayer.application, Culture=en, PublicKeyToken={PublishInfo.ApplicationIdentity.PublicKeyToken}, processorArchitecture={PublishInfo.ApplicationIdentity.ProcessorArchitecture.ToString().ToLower()}";
					File.WriteAllText(Logic.LocalDataDirectory + "Bivrost360Player.appref-ms", appref, Encoding.GetEncoding(1200));
                }
			} catch(Exception)
			{

			}
		}

		public static void AssociateFileExtensions()
		{

			//PROTOCOL REGISTRATION
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
				//commandKey.SetValue("","\"" + System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Substring(8).Replace('/',Path.DirectorySeparatorChar) + "\" --bivrost-protocol \"%1\"");
				commandKey.SetValue("", "rundll32.exe dfshim.dll,ShOpenVerbShortcut " + Logic.LocalDataDirectory + "Bivrost360Player.appref-ms" +"|%1");
			} catch(Exception exc) { }


			////VIDEO FILES CONTEXT MENU
			//try
			//{
			//	RegistryKey bivrostMenuCommandKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\.mp4\Shell\Open in 360Player\Command", true);
			//	if (bivrostMenuCommandKey == null)
			//		bivrostMenuCommandKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\SystemFileAssociations\.mp4\Shell\Open in 360Player\Command");

			//	RegistryKey bivrostMenuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\.mp4\Shell\Open in 360Player", true);				
			//	string iconSourcePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Graphics\\fileassoc.ico");
			//	bivrostMenuKey.SetValue("Icon", iconSourcePath);

			//	bivrostMenuCommandKey.SetValue("", "rundll32.exe dfshim.dll,ShOpenVerbShortcut " + Logic.LocalDataDirectory + "Bivrost360Player.appref-ms" + "|%1");

			//	bivrostMenuCommandKey.Close();
			//	bivrostMenuKey.Close();
			//}
			//catch (Exception exc) {
			//	//MessageBox.Show(exc.Message + "\n\n" + exc.StackTrace);
			//}
			AssociateExtension(".mp4");
			AssociateExtension(".m4v");
			AssociateExtension(".mov");
			AssociateExtension(".avi");
			AssociateExtension(".wmv");
		}

		private static void AssociateExtension(string extension)
		{
			if (!extension.StartsWith(".")) extension = "." + extension;

			//VIDEO FILES CONTEXT MENU
			try
			{
				RegistryKey bivrostMenuCommandKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\" + extension + @"\Shell\Open in 360Player\Command", true);
				if (bivrostMenuCommandKey == null)
					bivrostMenuCommandKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\SystemFileAssociations\" + extension + @"\Shell\Open in 360Player\Command");

				RegistryKey bivrostMenuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\" + extension + @"\Shell\Open in 360Player", true);
				string iconSourcePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "Graphics\\fileassoc.ico");
				bivrostMenuKey.SetValue("Icon", iconSourcePath);

				bivrostMenuCommandKey.SetValue("", "rundll32.exe dfshim.dll,ShOpenVerbShortcut " + Logic.LocalDataDirectory + "Bivrost360Player.appref-ms" + "|%1");

				bivrostMenuCommandKey.Close();
				bivrostMenuKey.Close();
			}
			catch (Exception exc)
			{
				//MessageBox.Show(exc.Message + "\n\n" + exc.StackTrace);
			}
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

		private static string GetPublicKeyTokenFromAssembly(Assembly assembly)
		{
			var bytes = assembly.GetName().GetPublicKeyToken();
			if (bytes == null || bytes.Length == 0)
				return "None";
			var publicKeyToken = string.Empty;
			for (int i = 0; i < bytes.GetLength(0); i++)
				publicKeyToken += string.Format("{0:x2}", bytes[i]);
			return publicKeyToken;
		}

		private static IntPtr GetPlayerWindow()
		{
			IntPtr playerPointer = IntPtr.Zero;

			NativeMethods.EnumWindows((in1, in2) =>
			{
				StringBuilder sb = new StringBuilder(256);
				NativeMethods.GetWindowText(in1, sb, 256);
				string text = sb.ToString();
				if (text.StartsWith("Bivrost 360Player"))
				{
					StringBuilder sbname = new StringBuilder(256);
					NativeMethods.GetClassName(in1, sbname, 256);
					logger.Info("Found Bivrost player window with class: " + sbname);
					playerPointer = in1;
				}
				return true;
			}, IntPtr.Zero);

			return playerPointer;
		}
	}
}
