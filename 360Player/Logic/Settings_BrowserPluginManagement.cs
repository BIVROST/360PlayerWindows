#if FEATURE_BROWSER_PLUGINS

using Bivrost.Bivrost360Player.ConfigUI;
using Newtonsoft.Json;


namespace Bivrost.Bivrost360Player
{
	public partial class Settings
	{
		public bool BrowserPluginQuestionShown { get; set; } = false;
		public bool BrowserPluginAccepted { get; set; } = false;

		[JsonIgnore]
		[SettingsProperty("Install browsers plugins", ConfigItemType.Action, Caption = "Install plugins")]
		public System.Action InstallPlugins {
			get {
				return () =>
				{
					var settings = Logic.Instance.settings;
					settings.BrowserPluginQuestionShown = false;
					BrowserPluginManagement.CheckForBrowsers();
				};
			}
		}
		

	}
}

#endif