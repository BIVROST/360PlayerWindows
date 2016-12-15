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
		[JsonIgnore]
		public string SettingsFile
		{
			get { return _settingsFile; }
			private set { _settingsFile = value; }
		}

		public Settings(string configFile = "")
		{
			if (string.IsNullOrWhiteSpace(configFile))
			{
				configFile = Logic.LocalDataDirectory + "settings.conf"; //Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BivrostPlayer\\settings.conf";
				//if (!Directory.Exists(Path.GetDirectoryName(configFile)))
				//	Directory.CreateDirectory(Path.GetDirectoryName(configFile));
			}
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


		public bool BrowserPluginQuestionShown { get; set; } = false;
		public bool BrowserPluginAccepted { get; set; } = false;
		public Guid InstallId { get; set; } = Guid.Empty;


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

		[SettingsProperty("Default VR headset mode", ConfigItemType.Enum)]
		public HeadsetMode HeadsetUsage { get; set; } = HeadsetMode.Auto;

		[SettingsProperty("OSVR screen number", ConfigItemType.Enum)]
		public ScreenSelection OSVRScreen { get; set; } = ScreenSelection.Autodetect;

		[SettingsProperty("Face screen in OpenVR", ConfigItemType.Bool)]
		public bool OpenVRReverse { get; set; } = true;

		[JsonIgnore]
		[SettingsAdvancedProperty("Reset Analytics ID", ConfigItemType.Action, Caption = "Reset ID")]
		public System.Action ResetInstallId { get; set; } = () => { };

		[JsonIgnore]
		[SettingsAdvancedProperty("Reset player configuration", ConfigItemType.Action, Caption = "Reset")]
		public System.Action ResetConfiguration { get; set; } = () => { };

		[JsonIgnore]
		[SettingsProperty("Install browsers plugins", ConfigItemType.Action, Caption = "Install plugins")]
		public System.Action InstallPlugins { get; set; } = () => { };

		[SettingsAdvancedProperty("User headset tracking in window", ConfigItemType.Bool)]
		public bool UserHeadsetTracking { get; set; } = false;



		//GhostVR and heatmaps
		[SettingsAdvancedProperty("Enable GhostVR analytics", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.ghostVR | FeaturesEnum.heatmaps)]
		public bool GhostVREnabled
		{
			get { return _ghostVREnabled && Features.GhostVR && Features.Heatmaps; }
			set { _ghostVREnabled = value; }
		}
		[JsonIgnore]
		bool _ghostVREnabled = false;


		[SettingsAdvancedProperty("GhostVR license token", ConfigItemType.String, requiredFeatures = FeaturesEnum.ghostVR | FeaturesEnum.heatmaps)]
		public string GhostVRLicenseToken { get; set; }


		[SettingsAdvancedProperty("Save local heatmaps", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.heatmaps)]
		public bool LocalHeatmaps
		{
			get { return _localHeatmaps && Features.Heatmaps; }
			set { _localHeatmaps = value; }
		}
		[JsonIgnore]
		bool _localHeatmaps = false;


		//License settings
		public string LicenseCode = "";


		//Remote control settings
		[SettingsAdvancedProperty("Enable http remote control (requires restart)", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.remote)]
		public bool EnableRemoteControl
		{
			get { return _enableRemoteControl && Features.RemoteEnabled; }
			set { _enableRemoteControl = value; }
		}
		[JsonIgnore]
		bool _enableRemoteControl = false;


		[SettingsAdvancedProperty("Movie directory for remote control playback", ConfigItemType.Path, requiredFeatures = FeaturesEnum.remote)]
		public string RemoteControlMovieDirectory { get; set; } = "";



		// Debug features
		[SettingsAdvancedProperty("Use black background in player window", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.isDebug)]
		public bool UseBlackBackground { get; set; } = false;

		[SettingsAdvancedProperty("Do not exit fullscreen on movie stop", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.isDebug)]
		public bool DoNotExitFullscreenOnStop { get; set; } = false;

		[SettingsAdvancedProperty("Disable UI", ConfigItemType.Bool, requiredFeatures = FeaturesEnum.isDebug)]
		public bool DisableUI { get; set; } = false;

	}

}