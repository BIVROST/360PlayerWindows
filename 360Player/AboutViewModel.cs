using Caliburn.Micro;
using Bivrost.Bivrost360Player.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	public class AboutViewModel : Screen
	{
		public AboutViewModel()
		{
			DisplayName = "About BIVROST 360Player";
			try
			{
				if(Tools.PublishInfo.ApplicationIdentity != null)
					Version = Tools.PublishInfo.ApplicationIdentity.Version.ToString();
			}
			catch (Exception) { }
			if(string.IsNullOrWhiteSpace(Version))
			{
				if (Assembly.GetExecutingAssembly() != null)
					Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				else
					Version = "( not supported )";
			}
		}

		protected override void OnViewReady(object view)
		{
			base.OnViewReady(view);
			IconHelper.RemoveIcon(view as System.Windows.Window);
		}

		public void OpenEULA()
		{
			DialogHelper.ShowDialog<EULAViewModel>();
		}

		public void ShowLibs()
		{
			DialogHelper.ShowDialog<EulaLibsViewModel>();
		}

		public string Version { get; set; }


		public void ContactSupport()
		{
			//mailto:someone@example.com?subject=This%20is%20the%20subject&cc=someone_else@example.com&body=This%20is%20the%20body
			System.Diagnostics.Process.Start("mailto:support@bivrost360.com");
			//SystemInfo.Info();
		}

		public void ContactCommercial()
		{
			System.Diagnostics.Process.Start("mailto:contact@bivrost360.com");
		}
	}
}
