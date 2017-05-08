using Bivrost.Log;
using Caliburn.Micro;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;

namespace PlayerUI.Statistics
{
	/// <summary>
	/// Interaction logic for SendStatistics.xaml
	/// </summary>
	public partial class SendStatistics : Window
	{

		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		[ComVisible(true)]
		public class ScriptingHelper
		{
			public event System.Action OnCanceled;
			public event System.Action OnCompleted;

			public void ActionCanceled() { OnCanceled?.Invoke(); }
			public void ActionCompleted() { OnCompleted?.Invoke(); }
		}


		enum UIMode { loading, form, failure }
		UIMode uiMode {
			set
			{
				Execute.OnUIThread(() =>
				{
					loading.Visibility = value == UIMode.loading ? Visibility.Visible : Visibility.Hidden;
					failed.Visibility = value == UIMode.failure ? Visibility.Visible : Visibility.Hidden;
					browser.Visibility = value == UIMode.form ? Visibility.Visible : Visibility.Hidden;

					cancel.Visibility = value != UIMode.failure ? Visibility.Visible : Visibility.Hidden;
					close.Visibility = value == UIMode.failure ? Visibility.Visible : Visibility.Hidden;
				});
			}
		}


		protected SendStatistics(Session session, GhostVRConnector ghostVRConnector)
		{
			InitializeComponent();
			close.Click += (s, e) => Close();
			cancel.Click += (s, e) => Close();
			uiMode = UIMode.loading;
			ghostVRConnector.VideoSession(session, SessionSendSuccess, SessionSendFailure);
		}

		private void SessionSendSuccess(string followUpUrl)
		{
			Execute.OnUIThread(() => 
			{
				uiMode = UIMode.form;
				browser.Navigate(followUpUrl);
				ScriptingHelper helper = new ScriptingHelper();
				//browser.ContextMenu = false;
				browser.ObjectForScripting = helper;
				helper.OnCanceled += Helper_OnCanceled;
				helper.OnCompleted += Helper_OnCompleted;
			});
		}


		private void SessionSendFailure(string obj)
		{
			uiMode = UIMode.failure;
		}
		

		internal static void Send(Session session, GhostVRConnector ghostVRConnector)
		{
			Execute.OnUIThreadAsync(() =>
			{
				var ss = new SendStatistics(session, ghostVRConnector);
				ss.Owner = ShellViewModel.Instance.shellView;
				ss.ShowDialog();
			});
		}


		private void Helper_OnCompleted()
		{
			Logger.Info("GhostVR form said it was completed");
			Close();
		}

		private void Helper_OnCanceled()
		{
			Logger.Info("GhostVR form said it was canceled");
			Close();
		}

	}


	public class GhostVRSessionSink : ISessionSink {
		private GhostVRConnector ghostVRConnector;

		public GhostVRSessionSink(GhostVRConnector ghostVRConnector)
		{
			this.ghostVRConnector = ghostVRConnector;
		}

		public void UseSession(Session session)
		{
			SendStatistics.Send(session, ghostVRConnector);
		}

		public bool Enabled
		{
			get
			{
				return Logic.Instance.settings.GhostVREnabled && Logic.Instance.ghostVRConnector.IsConnected;
			}
		}
	}
}
