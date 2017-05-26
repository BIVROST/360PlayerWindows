using Caliburn.Micro;
using System;
using System.Windows;

namespace PlayerUI.VideoAnalytics
{
	public class AnaliticsMenuViewModel : PropertyChangedBase
	{

		public AnaliticsMenuViewModel()
		{
			Features.ListUpdated += () =>
			{
				NotifyOfPropertyChange(nameof(AnaliticsMenuActive));
				NotifyOfPropertyChange(nameof(LocalSessionsAvailable));
				NotifyOfPropertyChange(nameof(GhostVRAvailable));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndConnected));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndDisconnected));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndPending));
				NotifyOfPropertyChange(nameof(GhostVREnabled));
				NotifyOfPropertyChange(nameof(GhostVRLabel));
			};

			ghostVRConnector.StatusChanged += (status) =>
			{
				NotifyOfPropertyChange(nameof(GhostVRAvailable));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndConnected));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndDisconnected));
				NotifyOfPropertyChange(nameof(GhostVRAvailableAndPending));
				NotifyOfPropertyChange(nameof(GhostVREnabled));
				NotifyOfPropertyChange(nameof(GhostVRLabel));
			};
		}


		public bool AnaliticsMenuActive { get { return LocalSessionsAvailable || GhostVRAvailable; } }

		public void AnalyticsAbout()
		{
			MessageBox.Show("This feature is not yet publically available.\r\nPlease contact support for details");
		}

		public bool LocalSessionsAvailable { get { return Features.LocallyStoredSessions; } }

		public bool LocalSessionsEnabled
		{
			get { return Logic.Instance.settings.LocallyStoredSessions; }
			set { Logic.Instance.settings.LocallyStoredSessions = value; }
		}

		public void LocalSessionsSetDirectory()
		{
			VideoAnalytics.LocallyStoredSessionSink lss = Logic.Instance.locallyStoredSessions;
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select directory to store local sessions", SelectedPath = lss.DestinationDirectory })
			{
				var result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
				{
					lss.DestinationDirectory = dialog.SelectedPath;
				}
			}
		}

		private VideoAnalytics.GhostVRConnector ghostVRConnector { get { return Logic.Instance.ghostVRConnector; } }
		public bool GhostVRAvailable { get { return Features.GhostVR; } }
		public bool GhostVRAvailableAndDisconnected { get { return GhostVRAvailable && ghostVRConnector.status == GhostVRConnector.ConnectionStatus.disconnected; } }
		public bool GhostVRAvailableAndConnected { get { return GhostVRAvailable && ghostVRConnector.status == GhostVRConnector.ConnectionStatus.connected; } }
		public bool GhostVRAvailableAndPending { get { return GhostVRAvailable && ghostVRConnector.status == GhostVRConnector.ConnectionStatus.pending; } }

		public bool GhostVREnabled
		{
			get { return Logic.Instance.settings.GhostVREnabled; }
			set { Logic.Instance.settings.GhostVREnabled = value; }
		}

		public string GhostVRLabel
		{
			get
			{
				switch (ghostVRConnector.status)
				{
					case VideoAnalytics.GhostVRConnector.ConnectionStatus.connected:
						return $"GhostVR: connected to team {ghostVRConnector.Name}";
					case VideoAnalytics.GhostVRConnector.ConnectionStatus.pending:
						return $"GhostVR: waiting for connection...";
					case VideoAnalytics.GhostVRConnector.ConnectionStatus.disconnected:
						return $"GhostVR: not connected";
					default: throw new ArgumentOutOfRangeException();
				}
			}
		}

		public void GhostVRConnect() { ghostVRConnector.Connect(); }
		public void GhostVRCancel() { ghostVRConnector.Cancel(); }
		public void GhostVRDisconnect() { ghostVRConnector.Disconnect(); }
	}
}
