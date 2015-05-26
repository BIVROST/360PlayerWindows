using System;
using System.Collections.Generic;
using System.Linq;
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

		public Logic()
		{
			settings = new Settings();
		}

		public void ReloadPlayer()
		{
			System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
			Application.Current.Shutdown();
		}
	}
}
