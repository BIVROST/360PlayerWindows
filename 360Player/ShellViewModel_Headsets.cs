// Part of ShellViewModel dedicated to headset support
// TODO: extract these

using Bivrost.Log;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bivrost.Bivrost360Player
{
	partial class ShellViewModel
	{
		// ShellViewModel.cs#142
		Oculus.OculusPlayback oculusPlayback;
		OSVRKit.OSVRPlayback osvrPlayback;
		OpenVR.OpenVRPlayback openVRPlayback;
		Headset _currentHeadset = null;
		public Headset CurrentHeadset
		{
			get { return _currentHeadset; }
			protected set
			{
				if (value == _currentHeadset)
					return;
				if (_currentHeadset != null)
					HeadsetDisable?.Invoke(_currentHeadset);
				_currentHeadset = value;
				if (_currentHeadset != null)
					HeadsetEnable?.Invoke(_currentHeadset);
			}
		}
		public event Action<Headset> HeadsetDisable;
		public event Action<Headset> HeadsetEnable;
		List<Headset> headsets;


		// ShellViewModel.cs#195
		void InitHeadsets()
		{
			headsets = new List<Headset>()
			{
				(oculusPlayback = new Oculus.OculusPlayback()),
				(osvrPlayback = new OSVRKit.OSVRPlayback()),
				(openVRPlayback = new OpenVR.OpenVRPlayback())
			};

			HeadsetMenu = new HeadsetMenuViewModel();
			HeadsetMenu.OnRift += () => HeadsetIsOculus = true;
			HeadsetMenu.OnOSVR += () => HeadsetIsOSVR = true;
			HeadsetMenu.OnVive += () => HeadsetIsOpenVR = true;
			HeadsetMenu.OnDisable += () => HeadsetIsDisable = true;
		}

		// ShellViewModel.cs#260

		void VRUIUpdatePlaybackTime(double time) {
			headsets.ForEach(h => h.UpdateTime((float)time));
		}


		void VRUIPause() { headsets.ForEach(h => h.Pause()); }


		void VRUIUnpause() { headsets.ForEach(h => h.UnPause()); }


		// ShellViewModel.cs#636
		void ResetVR()
		{
			Logger log = new Logger("ResetVR");

			CurrentHeadset?.Stop();

			CurrentHeadset = null;

			while (headsets.Any(h => h.Lock))
			{
				Thread.Sleep(50);
			}

			headsets.ForEach(h => h.Reset());

			if (SettingHeadsetUsage == HeadsetMode.Oculus)
			{
				try
				{
					if (oculusPlayback.IsPresent())
					{
						Logic.Notify("Oculus Rift detected. Starting VR playback...");
						oculusPlayback.textureL = _mediaDecoder.TextureL;
						oculusPlayback.textureR = _mediaDecoder.TextureR;
						oculusPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
						oculusPlayback._projection = _mediaDecoder.Projection;
						oculusPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
						oculusPlayback.Start();
						ShellViewModel.SendEvent("headsetConnected", "oculus");
						CurrentHeadset = oculusPlayback;
						return;
					}
				}
				catch (Exception e)
				{
					log.Error("Headset detection exception (Oculus): " + e);
				}
				Logic.Notify("Oculus Rift not detected.");
				ShellViewModel.SendEvent("headsetError", "oculus");
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "Oculus Rift");
			}

			if (SettingHeadsetUsage == HeadsetMode.OpenVR)
			{
				try
				{
					if (openVRPlayback.IsPresent())
					{
						Logic.Notify("OpenVR detected. Starting VR playback...");
						openVRPlayback.textureL = _mediaDecoder.TextureL;
						openVRPlayback.textureR = _mediaDecoder.TextureR;
						openVRPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
						openVRPlayback._projection = _mediaDecoder.Projection;
						openVRPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
						openVRPlayback.Start();
						ShellViewModel.SendEvent("headsetConnected", "openvr");
						CurrentHeadset = openVRPlayback;
						return;
					}
				}
				catch (Exception e)
				{
					log.Error("Headset detection exception (OpenVR): " + e);
				}
				Logic.Notify("OpenVR not detected.");
				ShellViewModel.SendEvent("headsetError", "openvr");
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "OpenVR");
			}

			if (SettingHeadsetUsage == HeadsetMode.OSVR)
			{
				if (osvrPlayback.IsPresent())
				{
					Logic.Notify("OSVR detected. Starting VR playback...");
					osvrPlayback.textureL = _mediaDecoder.TextureL;
					osvrPlayback.textureR = _mediaDecoder.TextureR;
					osvrPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
					osvrPlayback._projection = _mediaDecoder.Projection;
					osvrPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
					osvrPlayback.Start();
					ShellViewModel.SendEvent("headsetConnected", "osvr");
					CurrentHeadset = osvrPlayback;
					return;
				}
				Logic.Notify("OSVR not detected.");
				ShellViewModel.SendEvent("headsetError", "osvr");
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "OSVR");
			}
		}


		void UpdateVRSceneSettings(MediaDecoder.ProjectionMode projectionMode, MediaDecoder.VideoMode videoMode)
		{
			CurrentHeadset?.UpdateSceneSettings(projectionMode, videoMode);
		}


		public HeadsetMode SettingHeadsetUsage
		{
			get { return Logic.Instance.settings.HeadsetUsage; }
			set
			{
				SetHeadset(value);
			}
		}


		#region menu opions: headset
		public void SetHeadset(HeadsetMode headset)
		{
			if (Logic.Instance.settings.HeadsetUsage == headset)
				return;

			LoggerManager.Info($"Set headset: {headset} (menu option)");
			Logic.Instance.settings.HeadsetUsage = headset;
			Logic.Instance.settings.Save();

			switch (headset)
			{
				case HeadsetMode.Oculus: Logic.Notify("Oculus Rift playback selected."); break;
				case HeadsetMode.OSVR: Logic.Notify("OSVR playback selected."); break;
				case HeadsetMode.OpenVR: Logic.Notify("OpenVR (SteamVR) playback selected."); break;
				case HeadsetMode.Disable: Logic.Notify("Headset playback disabled."); break;
			}

			NotifyOfPropertyChange(() => HeadsetIsOculus);
			NotifyOfPropertyChange(() => HeadsetIsOpenVR);
			NotifyOfPropertyChange(() => HeadsetIsOSVR);
			NotifyOfPropertyChange(() => HeadsetIsDisable);

			ResetVR();
		}


		public bool HeadsetIsOculus
		{
			get { return Logic.Instance.settings.HeadsetUsage == HeadsetMode.Oculus; }
			set { if (value) SetHeadset(HeadsetMode.Oculus); }
		}
		public bool HeadsetIsOpenVR
		{
			get { return Logic.Instance.settings.HeadsetUsage == HeadsetMode.OpenVR; }
			set { if (value) SetHeadset(HeadsetMode.OpenVR); }
		}
		public bool HeadsetIsOSVR
		{
			get { return Logic.Instance.settings.HeadsetUsage == HeadsetMode.OSVR; }
			set { if (value) SetHeadset(HeadsetMode.OSVR); }
		}
		public bool HeadsetIsDisable
		{
			get { return Logic.Instance.settings.HeadsetUsage == HeadsetMode.Disable; }
			set { if (value) SetHeadset(HeadsetMode.Disable); }
		}
		#endregion



		public bool UserHeadsetTracking
		{
			get { return Logic.Instance.settings.UserHeadsetTracking; }
			set
			{
				if (Logic.Instance.settings.UserHeadsetTracking == value) return;
				Logic.Instance.settings.UserHeadsetTracking = value;
				NotifyOfPropertyChange(nameof(UserHeadsetTracking));
			}
		}

		public void ToggleUserHeadsetTracking()
		{
			UserHeadsetTracking = !UserHeadsetTracking;
		}
	}
}