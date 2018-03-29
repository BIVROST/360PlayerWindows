using Bivrost.Bivrost360Player.Tools;
using Bivrost.MOTD;

namespace Bivrost.Bivrost360Player
{
	public partial class ShellViewModel
	{
		private class MOTDBridge : IMOTDBridge
		{
			public string InstallId => Logic.Instance.settings.InstallId.ToString();

			public string Version => PublishInfo.ApplicationIdentity?.Version.ToString();

			public string Product => Logic.productCode;

			public void DisplayNotification(string text)
			{
				Logic.Notify(text);
			}

			public void DisplayNotification(string text, string link, string url)
			{
				if (url == "::update::")
				{
					var notification = new NotificationViewModel(text, Updater.InstallUpdate, link);
					Caliburn.Micro.Execute.OnUIThreadAsync(
						() => ShellViewModel.Instance?.NotificationCenter?.PushNotification(notification)
					);
				}
				else
				{
					Logic.NotifyWithLink(text, url, link);
				}
			}

			public void DisplayPopup(string title, string url, int width = 600, int height = 400)
			{
				MOTDPopup.ShowPopup(title, url, width, height, Updater.InstallUpdate);
			}
		}
	}

}
