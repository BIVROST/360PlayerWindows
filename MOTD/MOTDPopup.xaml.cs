using Caliburn.Micro;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using Bivrost.Log;
using Bivrost.Bivrost360Player;

namespace Bivrost.MOTD
{
	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	[ComVisible(true)]
	public class ScriptingHelper
	{
		public event System.Action OnUpdateRequested;
		public event System.Action OnRequestClose;

		public void RequestUpdate() { OnUpdateRequested?.Invoke(); }
		public void RequestClose() { OnRequestClose?.Invoke(); }
	}




	/// <summary>
	/// Interaction logic for MOTDPopup.xaml
	/// </summary>
	public partial class MOTDPopup : Window
	{
		private readonly Logger logger;

		internal MOTDPopup(string title, string url, int width, int height, object scriptingHelper)
		{
			logger = MOTD.MOTDClient.logger;
			Title = title;
			Width = width;
			Height = height;
			InitializeComponent();
			//browser.LoadCompleted += (s, e) => ...
			browser.Navigate(url);
			browser.ObjectForScripting = scriptingHelper;
		}


		public static void ShowPopup(string title, string url, object scriptingHelper, int width = 600, int height = 400)
		{
			Execute.OnUIThreadAsync(() =>
			{
				var popup = new MOTDPopup(title, url, width, height, scriptingHelper);

				// fix: when main window is closed, but another window is still active, app would not turn off without this
				if (ShellViewModel.Instance.shellView.IsActive)
					popup.Owner = ShellViewModel.Instance.shellView;
				popup.ShowDialog();
			});
		}


		public static void ShowPopup(string title, string url, int width = 600, int height = 400, System.Action requestUpdate = null)
		{
			Execute.OnUIThreadAsync(() =>
			{
				var helper = new ScriptingHelper();
				var popup = new MOTDPopup(title, url, width, height, helper);

				helper.OnRequestClose += () =>
				{
					popup.logger.Info("Page said it wants to close");
					popup.Close();
				};

				helper.OnUpdateRequested += () => 
				{
					popup.logger.Info("Form said it wants to update");
					popup.Close();
				};
				//< close anyway even if requestUpdate provided
				if (requestUpdate != null)
					helper.OnUpdateRequested += requestUpdate;

				// fix: when main window is closed, but another window is still active, app would not turn off without this
				if (ShellViewModel.Instance.shellView.IsActive)
					popup.Owner = ShellViewModel.Instance.shellView;
				popup.ShowDialog();
			});
		}
		

	}

}
