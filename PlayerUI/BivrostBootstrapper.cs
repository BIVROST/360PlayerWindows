using Caliburn.Micro;
using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
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

			if (mutex.WaitOne(TimeSpan.Zero, true))
			{
				ownMutex = true;
				if (Logic.Instance.settings.EventMode)
					DisplayRootViewFor<EventShellViewModel>();
				else
					DisplayRootViewFor<ShellViewModel>();				
			}
			else
			{
				string[] args = Environment.GetCommandLineArgs();

				if (args.Length > 1)
					if (!string.IsNullOrWhiteSpace(args[1]))
					{
						Clipboard.SetText(args[1]);
						NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWBIVROSTPLAYER, IntPtr.Zero, IntPtr.Zero);
					}
				Application.Shutdown();
			}

			//if (Logic.Instance.settings.EventMode)
			//	DisplayRootViewFor<EventShellViewModel>();
			//else
			//	DisplayRootViewFor<ShellViewModel>();
		}

		protected override void OnExit(object sender, EventArgs e)
		{
			base.OnExit(sender, e);
			if(ownMutex)
				mutex.ReleaseMutex();
		}
	}
}
