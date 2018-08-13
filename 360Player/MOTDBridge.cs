using Bivrost.Bivrost360Player.Tools;
using Bivrost.MOTD;
using System.Deployment;
using System.Windows;
using System.Deployment.Application;
using Windows.ApplicationModel;
using System;
using System.Reflection;

namespace Bivrost.Bivrost360Player
{
	public partial class ShellViewModel
	{
		private class MOTDBridge : IMOTDBridge
		{
			public string InstallId => Logic.Instance.settings.InstallId.ToString();

			public string Version
			{
				get
				{
					try
					{
						if (ApplicationDeployment.IsNetworkDeployed)
							return PublishInfo.ApplicationIdentity?.Version.ToString();
					}
					catch (Exception exc) { }

					try
					{
						var version = Package.Current.Id.Version;
						return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, 0, version.Build);
					} catch(Exception exc) { }

					var assembly = typeof(App).GetTypeInfo().Assembly;
					var assemblyVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
					return assemblyVersion;
				}
			}

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
