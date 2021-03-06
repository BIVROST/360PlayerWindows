﻿using Bivrost.Log;
using Caliburn.Micro;
using System;
using System.Windows;
using System.Windows.Input;

using Bivrost.Bivrost360Player;

namespace Bivrost.AnalyticsForVR
{
	public class AnaliticsMenuViewModel : PropertyChangedBase
	{

		public AnaliticsMenuViewModel()
		{
			Features.ListUpdated += () => Execute.OnUIThreadAsync(UpdateAllProperties);
#if FEATURE_GHOSTVR
			ghostVRConnector.StatusChanged += status => Execute.OnUIThreadAsync(UpdateAllProperties);
#endif
		}


		private void UpdateAllProperties()
		{
			NotifyOfPropertyChange(nameof(AnaliticsMenuActive));
			NotifyOfPropertyChange(nameof(LocalSessionsAvailable));

			NotifyOfPropertyChange(nameof(GhostVRAvailable));
			NotifyOfPropertyChange(nameof(GhostVRAvailableAndConnected));
			NotifyOfPropertyChange(nameof(GhostVRAvailableAndDisconnected));
			NotifyOfPropertyChange(nameof(GhostVRAvailableAndPending));
			NotifyOfPropertyChange(nameof(GhostVREnabled));
			NotifyOfPropertyChange(nameof(GhostVRLabel));

			// update commands
			CommandManager.InvalidateRequerySuggested();
		}


		public bool AnaliticsMenuActive { get { return LocalSessionsAvailable || GhostVRAvailable; } }

		public void AboutAnalytics()
		{
			MessageBox.Show("This feature is not yet publically available.\r\nPlease contact support for details");
		}

		public bool LocalSessionsAvailable { get { return Features.LocallyStoredSessions; } }

		public bool LocalSessionsEnabled
		{
			get { return Logic.Instance.settings.LocallyStoredSessions; }
			set { Logic.Instance.settings.LocallyStoredSessions = value; }
		}

		void LocalSessionsSetDirectory()
		{
			LocallyStoredSessionSink lss = Logic.Instance.locallyStoredSessions;
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select directory to store local sessions", SelectedPath = lss.DestinationDirectory })
			{
				var result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
				{
					lss.DestinationDirectory = dialog.SelectedPath;
				}
			}
		}


		public RelayCommand CommandAboutAnalitics => new RelayCommand(_ => AboutAnalytics(), _ => AnaliticsMenuActive);

		public RelayCommand CommandLocalSessionsSetDirectory => new RelayCommand(_ => LocalSessionsSetDirectory(), _ => LocalSessionsAvailable);


#if FEATURE_GHOSTVR
		private static GhostVRConnector ghostVRConnector { get { return Logic.Instance.ghostVRConnector; } }
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
					case GhostVRConnector.ConnectionStatus.connected:
						return $"GhostVR: connected to team {ghostVRConnector.Name}";
					case GhostVRConnector.ConnectionStatus.pending:
						return $"GhostVR: waiting for connection...";
					case GhostVRConnector.ConnectionStatus.disconnected:
						return $"GhostVR: not connected";
					default: throw new ArgumentOutOfRangeException();
				}
			}
		}

		public RelayCommand CommandGhostVRDisconnect => new RelayCommand(
			_ => ghostVRConnector.Disconnect(),
			_ => GhostVRAvailableAndConnected
		);

		public RelayCommand CommandGhostVRConnect => new RelayCommand(
			_ => ghostVRConnector.Connect(), 
			_ => GhostVRAvailableAndDisconnected
		);

		public RelayCommand CommandGhostVRCancel => new RelayCommand(
			_ => ghostVRConnector.Cancel(),
			_ => GhostVRAvailableAndPending
		);
#else
		public bool GhostVRAvailable { get { return false; } }
		public bool GhostVRAvailableAndDisconnected { get { return false; } }
		public bool GhostVRAvailableAndConnected { get { return false; } }
		public bool GhostVRAvailableAndPending { get { return false; } }

		public bool GhostVREnabled
		{
			get { return false; }
			set { ; }
		}

		public string GhostVRLabel { get { return "GhostVR is disabled"; } }

		public RelayCommand CommandGhostVRDisconnect => new RelayCommand(_ => { }, _ => false);
		public RelayCommand CommandGhostVRConnect => new RelayCommand(_ => { }, _ => false);
		public RelayCommand CommandGhostVRCancel => new RelayCommand(_ => { }, _ => false);
#endif


	}
}
