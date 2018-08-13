// Part of ShellViewModel dedicated to headset support
// TODO: extract these

using Bivrost.Log;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	partial class ShellViewModel
	{
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
			var oculus = new Oculus.OculusPlayback();
			var osvr = new OSVRKit.OSVRPlayback();
			var openvr = new OpenVR.OpenVRPlayback();

			osvr.OnGotFocus += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() => shellView.Activate());
			});

			headsets = new List<Headset>()
			{
				oculus,
				osvr,
				openvr
			};

			HeadsetMenu = new HeadsetMenuViewModel();
			HeadsetMenu.OnRift += () => HeadsetIsOculus = true;
			HeadsetMenu.OnOSVR += () => HeadsetIsOSVR = true;
			HeadsetMenu.OnVive += () => HeadsetIsOpenVR = true;
			HeadsetMenu.OnDisable += () => HeadsetIsDisable = true;
		}

		// ShellViewModel.cs#260

		void VRUIUpdatePlaybackTime(double time) { CurrentHeadset?.UpdateTime((float)time); }
		void VRUIPause() { CurrentHeadset?.Pause(); }
		void VRUIUnpause() { CurrentHeadset?.UnPause(); }


		void HeadsetStop() { CurrentHeadset?.Stop(); }

		HeadsetMode CurrentHeadsetMode
		{
			get
			{
				if (CurrentHeadset == null)
					return HeadsetMode.Disable;
				if (CurrentHeadset is Oculus.OculusPlayback)
					return HeadsetMode.Oculus;
				if (CurrentHeadset is OSVRKit.OSVRPlayback)
					return HeadsetMode.OSVR;
				if (CurrentHeadset is OpenVR.OpenVRPlayback)
					return HeadsetMode.OpenVR;

				throw new Exception("Unknown headset type");
			}
		}

		// ShellViewModel.cs#636
		void ResetVR()
		{
			Logger log = new Logger("ResetVR");

			if(CurrentHeadsetMode == SettingHeadsetUsage)
			{
				log.Info("Will not reset, because the headset is still alive.");
				return;
			}
			else
			{
				log.Info($"Headset change: {CurrentHeadsetMode} -> {SettingHeadsetUsage}");
			}

			CurrentHeadset?.Abort();

			CurrentHeadset = null;

			while (headsets.Any(h => h.Lock))
			{
				Thread.Sleep(50);
			}

			//	case HeadsetMode.Oculus: Logic.Notify("Oculus Rift playback selected."); break;
			//	case HeadsetMode.OSVR: Logic.Notify("OSVR playback selected."); break;
			//	case HeadsetMode.OpenVR: Logic.Notify("OpenVR (SteamVR) playback selected."); break;


			if (SettingHeadsetUsage == HeadsetMode.Oculus)
			{
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "Oculus Rift");
				try
				{
					Headset oculusPlayback = headsets.Find(h => h is Oculus.OculusPlayback);
					if (oculusPlayback.IsPresent())
					{
						Logic.Notify("Oculus Rift detected. Starting VR playback...");
						oculusPlayback.Media = SelectedServiceResult;
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
			}

			if (SettingHeadsetUsage == HeadsetMode.OpenVR)
			{
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "OpenVR");
				try
				{
					Headset openVRPlayback = headsets.Find(h => h is OpenVR.OpenVRPlayback);
					if (openVRPlayback.IsPresent())
					{
						Logic.Notify("OpenVR detected. Starting VR playback...");
						openVRPlayback.Media = SelectedServiceResult;
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
			}

			if (SettingHeadsetUsage == HeadsetMode.OSVR)
			{
				Logic.Instance.stats.TrackEvent("Application events", "Headset", "OSVR");
				Headset osvrPlayback = headsets.Find(h => h is OSVRKit.OSVRPlayback);
				if (osvrPlayback.IsPresent())
				{
					Logic.Notify("OSVR detected. Starting VR playback...");
					osvrPlayback.Media = SelectedServiceResult;
					osvrPlayback.Start();
					ShellViewModel.SendEvent("headsetConnected", "osvr");
					CurrentHeadset = osvrPlayback;
					return;
				}
				Logic.Notify("OSVR not detected.");
				ShellViewModel.SendEvent("headsetError", "osvr");
			}
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
		public async void SetHeadset(HeadsetMode headset)
		{
			if (Logic.Instance.settings.HeadsetUsage == headset)
				return;

			LoggerManager.Info($"Set headset: {headset} (menu option)");
			Logic.Instance.settings.HeadsetUsage = headset;
			Logic.Instance.settings.Save();

			shellView.headsetCheckDisable.IsEnabled = false;
			shellView.headsetCheckOculus.IsEnabled = false;
			shellView.headsetCheckOpenVR.IsEnabled = false;
			shellView.headsetCheckOSVR.IsEnabled = false;

			//switch (headset)
			//{
			//	case HeadsetMode.Oculus: Logic.Notify("Oculus Rift playback selected."); break;
			//	case HeadsetMode.OSVR: Logic.Notify("OSVR playback selected."); break;
			//	case HeadsetMode.OpenVR: Logic.Notify("OpenVR (SteamVR) playback selected."); break;
			//	case HeadsetMode.Disable: Logic.Notify("Headset playback disabled."); break;
			//}

			NotifyOfPropertyChange(() => HeadsetIsOculus);
			NotifyOfPropertyChange(() => HeadsetIsOpenVR);
			NotifyOfPropertyChange(() => HeadsetIsOSVR);
			NotifyOfPropertyChange(() => HeadsetIsDisable);


			await Task.Factory.StartNew(() =>
			{
				ResetVR();
			});

			shellView.headsetCheckDisable.IsEnabled = true;
			shellView.headsetCheckOculus.IsEnabled = true;
			shellView.headsetCheckOpenVR.IsEnabled = true;
			shellView.headsetCheckOSVR.IsEnabled = true;
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