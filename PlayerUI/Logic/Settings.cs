using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using PlayerUI.ConfigUI;

namespace PlayerUI
{
	public class Settings
	{
		#region settings management
		private string _settingsFile = "";
		private string SettingsFile
		{
			get { return _settingsFile; }
			set { _settingsFile = value; }
		}

		public Settings(string configFile = "")
		{
			if (string.IsNullOrWhiteSpace(configFile))
				configFile = System.Windows.Forms.Application.StartupPath + "settings.conf";
			SettingsFile = configFile;
			Load();
		}


		public void Load()
		{
			if (File.Exists(SettingsFile)) {
				JsonConvert.PopulateObject(File.ReadAllText(SettingsFile), this);
			}
			else
			{
				Save();
			}
		}

		public void Save()
		{
			File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(this));
			Console.WriteLine(JsonConvert.SerializeObject(this));
		}
		#endregion

		public bool EnableRemoteServer { get; set; } = false;
		public bool EventMode { get; set; } = false;		
		public string EventModeSingleFile { get; set; } = "";		
		public bool EventModeAutoPlay { get; set; } = true;
		public bool EventModePauseAtStartup { get; set; } = true;
		public string EventModeBackgroundColor { get; set; } = "000000";
		public bool EventModeLoop { get; set; } = true;

		public string AutoPlayFile { get; set; } = "";
		public bool AutoPlay { get; set; } = true;
		public bool AutoLoad { get; set; } = true;

		[SettingsProperty("Start in fullscreen", ConfigItemType.Bool)]
		public bool StartInFullScreen { get; set; } = false;

		[SettingsProperty("Use mouse to look around when Oculus Rift connected", ConfigItemType.Bool)]
		public bool UseMouseLookWithOculus { get; set; } = true;

		[SettingsProperty("Use Oculus Rift if available", ConfigItemType.Bool)]
		public bool UseOculusWhenConnected { get; set; } = true;


	}
}