using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

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

		public Settings(string configFile = "settings.conf")
		{
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
				Defauls();
				Save();
			}
		}

		public void Save()
		{
			File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(this));
			Console.WriteLine(JsonConvert.SerializeObject(this));
		}
		#endregion


		public bool EventMode { get; set; }
		public string EventModeSingleFile { get; set; }
		public bool EventModeAutoPlay { get; set; }
		public bool EventModePauseAtStartup { get; set; }
		public string EventModeBackgroundColor { get; set; }
		public bool EventModeLoop { get; set; }

		public void Defauls()
		{
			EventMode = false;
			EventModeSingleFile = "";
			EventModeAutoPlay = true;
			EventModePauseAtStartup = true;
			EventModeBackgroundColor = "000000";
			EventModeLoop = true;
		}
	}
}
