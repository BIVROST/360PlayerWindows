#if FEATURE_REMOTE_CONTROL

using Bivrost.Bivrost360Player.ConfigUI;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Bivrost.Bivrost360Player
{
	[RemoteControlSettingVerificator]
	public partial class Settings
	{
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

	}


	internal class RemoteControlSettingVerificatorAttribute:SettingVerificationAttribute
	{
		public override bool Valid => true;

		public override void Normalize() {
			if (!Directory.Exists(Logic.Instance.settings.RemoteControlMovieDirectory))
			{
				Logic.Instance.settings.RemoteControlMovieDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
				Logic.Instance.settings.Save();
			}
		}
	}
}

#endif
