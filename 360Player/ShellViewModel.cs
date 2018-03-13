using Caliburn.Micro;
using Bivrost.Bivrost360Player.ConfigUI;
using Bivrost.Bivrost360Player;
using Bivrost.Bivrost360Player.Tools;
using Bivrost.Bivrost360Player.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using Bivrost;
using System.Text.RegularExpressions;
using SharpDX.Direct3D11;
using SharpDX.XInput;
using LoggerManager = Bivrost.Log.LoggerManager;
using Bivrost.AnalyticsForVR;
using Bivrost.Licensing;
using Bivrost.Log;

namespace Bivrost.Bivrost360Player
{
	public partial class ShellViewModel : Screen
	{
		public static ShellViewModel Instance;

		public enum PlayerState
		{
			Idle,
			FileSelected,
			MediaLoaded,
			Playing,
			Ended
		}

		public string VideoLength { get; set; }
		public string CurrentPosition { get; set; }
		public string VideoTime { get; set; }



		public bool IsPlaying { get { return _mediaDecoder.IsPlaying; } }
		public bool IsPaused { get; set; }
		public bool Loop
		{
			get
			{
				return _mediaDecoder.Loop;
			}
			set
			{
				Console.WriteLine("Writing Loop: " + value);
				_mediaDecoder.Loop = value;
				NotifyOfPropertyChange(() => Loop);
			}
		}

		public string SelectedFileNameLabel
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(SelectedFileTitle)) return SelectedFileTitle;
				if (!string.IsNullOrWhiteSpace(SelectedFileName))
					if (SelectedFileName.ToLower().StartsWith("http")) return "web stream";
				return Path.GetFileNameWithoutExtension(SelectedFileName);
			}
		}

		private string _selectedFileName = "";
		public string SelectedFileName {
			get { return _selectedFileName; }
			set
			{
				this._selectedFileName = value;
				NotifyOfPropertyChange(() => SelectedFileName);
                if (value == null)
                    SelectedServiceResult = null;
			}
		}
        public Streaming.ServiceResult SelectedServiceResult
        {
            get; protected set;
        }

		public bool IsFileSelected { get; set; }

		public string SelectedFileTitle { get; set; } = "";
		public string SelectedFileDescription { get; set; } = "";

		public DPFCanvas DXCanvas;
		public ShellView shellView;

		private bool ended = false;
		private bool lockSlider = false;

		private bool autoplay = false;
		private MediaDecoder _mediaDecoder;
		private bool _ready = false;

		Window playerWindow;
		Nancy.Hosting.Self.NancyHost remoteControl;
		float remoteTime = 0;

		private AutoResetEvent waitForPlaybackReady = new AutoResetEvent(false);
		private ManualResetEvent waitForPlaybackStop = new ManualResetEvent(false);

		public string PlayerTitle {
			get
			{
				string title = "BIVROST 360Player ™ " + (Features.IsCanary ? "CANARY" : "BETA");
				if (IsPlaying)
					title += $" - now playing {SelectedFileNameLabel}";
				if (!Features.Commercial)
					title += " - FOR NON-COMMERCIAL USE";
				return title;
			}
		}


		public VolumeControlViewModel VolumeRocker { get; set; }
		public HeadsetMenuViewModel HeadsetMenu { get; set; }

		public static string FileFromArgs = "";

		private static TimeoutBool urlLoadLock = false;

		public NotificationCenterViewModel NotificationCenter { get; set; }


		public class LicensingContext : LicensingConnector.IContext
		{
			public string LicenseCode
			{
				get { return Logic.Instance.settings.LicenseCode; }
				set
				{
					Logic.Instance.settings.LicenseCode = value;
					Logic.Instance.settings.Save();
				}
			}


			public bool RequireLicense { get { return Features.RequireLicense; } }


			public string InstallId { get { return Logic.Instance.settings.InstallId.ToString(); } }


			public string ProductCode { get { return Logic.productCode; } }


			public void QuitApplication()
			{
				ShellViewModel.Instance.Quit();
			}


			public void LicenseUpdated(LicenseNinja.License license)
			{
				if (license == null)
					Features.SetBasicFeatures();
				else
					Features.SetFromLicense(license);
				Features.TriggerListUpdated();
			}


			public void LicenseVerified()
			{
				Logic.Notify("License verified");
			}

		}



		LicensingConnector licensingConnector = new LicensingConnector(new LicensingContext());

		public ShellViewModel()
		{
			InitHeadsets();

			ShellViewModel.Instance = this;
            ShellViewModel.OnInstantiated?.Invoke(this);

            var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);

			NotifyOfPropertyChange(() => PlayerTitle);

			CurrentPosition = "00:00:00";
			VideoLength = "00:00:00";

			NotificationCenter = new NotificationCenterViewModel();
			
			_mediaDecoder = new MediaDecoder();
			_mediaDecoder.Loop = Loop;


			_mediaDecoder.OnReady += (duration) =>
			{
				_ready = true;
				SendEvent("movieLoaded", Path.GetFileName(SelectedFileName));
				if (autoplay)
				{
					autoplay = false;
					urlLoadLock = false;
					Play();
				}
			};

			_mediaDecoder.OnEnded += () =>
			{
				SendEvent("movieEnded", Path.GetFileName(SelectedFileName));
				Task.Factory.StartNew(() => Execute.OnUIThread(() =>
				{
					Stop();
					ShowStartupUI();
				}));
			};

			_mediaDecoder.OnStop += () =>
			{
				Task.Factory.StartNew(() => waitForPlaybackStop.Set());
				SendEvent("movieStopped", Path.GetFileName(SelectedFileName));
			};

			_mediaDecoder.OnTimeUpdate += (time) =>
			{

				if (!_mediaDecoder.IsPlaying) return;

				Execute.OnUIThreadAsync(() =>
				{
					if (!lockSlider)
					{
						VRUIUpdatePlaybackTime(time);
						CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
						_timeValue = time;
						NotifyOfPropertyChange(() => TimeValue);
					}
					UpdateTimeLabel();
				});
			};

			_mediaDecoder.OnError += (error) =>
			{
				urlLoadLock = false;
				Execute.OnUIThreadAsync(() =>
				{
					NotificationCenter.PushNotification(MediaDecoderHelper.GetNotification(error));
					SelectedFileName = null;
                    SelectedServiceResult = null;
					ShowStartupUI();
				});
				SendEvent("playbackError", error);
			};

			_mediaDecoder.OnBufferingStarted += () =>
			{
				Execute.OnUIThreadAsync(() =>
				{
					shellView.BufferingStatus.Visibility = Visibility.Visible;
				});
			};

			_mediaDecoder.OnBufferingEnded += () =>
			{
				Execute.OnUIThreadAsync(() =>
				{
					shellView.BufferingStatus.Visibility = Visibility.Collapsed;
				});
			};

			_mediaDecoder.OnProgress += (progress) =>
			{
				Execute.OnUIThreadAsync(() =>
				{
					shellView.BufferingStatus.Text = $"Buffering... {progress}";
				});
			};


			UpdateTimeLabel();

			VolumeRocker = new VolumeControlViewModel();
			VolumeRocker.Volume = 0.5;
			VolumeRocker.OnVolumeChange += (volume) =>
			{
				_mediaDecoder.SetVolume(volume);
				ShellViewModel.SendEvent("volumeChanged", volume);
			};


			Logic.Instance.OnUpdateAvailable += () => Execute.OnUIThreadAsync(() =>
			{
				NotificationCenter.PushNotification(
					new NotificationViewModel(
						"A new version of Bivrost 360Player is available.",
						() =>
						{
							Updater.OnUpdateFail +=
								() => Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Something went wrong :(")));
							Updater.OnUpdateSuccess +=
								() => Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Update completed successfully.", () =>
								{
									System.Windows.Forms.Application.Restart();
									System.Windows.Application.Current.Shutdown();
								}, "restart", 60f)));
							Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Installing update...")));
							Task.Factory.StartNew(() => Updater.InstallUpdate());
						},
						"install now"
						)
					);
			});

			Logic.Instance.ValidateSettings();
		}


		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{

			if (msg == NativeMethods.WM_SHOWBIVROSTPLAYER)
			{
				BringToFront();
				handled = true;
			}
			if (msg == NativeMethods.WM_COPYDATA)
			{
				NativeMethods.COPYDATASTRUCT cps = (NativeMethods.COPYDATASTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(NativeMethods.COPYDATASTRUCT));
				string data = cps.lpData;

				BringToFront(data);
				handled = true;
			}

            InputDevices.NavigatorInputDevice.WndProc(msg, wParam, lParam); 

            return IntPtr.Zero;
		}

		public void BringToFront(string wmText = "")
		{
			Execute.OnUIThreadAsync(() => playerWindow.Activate());

			//string clipboardText = Clipboard.GetText();
			string clipboardText = wmText;

			LoggerManager.Info($"Clipboard: ${clipboardText}");

			OpenURI(clipboardText);
		}

		protected override void OnViewLoaded(object view)
		{
			
			base.OnViewLoaded(view);

            IntPtr windowHandle = new WindowInteropHelper(playerWindow).Handle;
            HwndSource source = HwndSource.FromHwnd(windowHandle);
			source.AddHook(new HwndSourceHook(WndProc));

			this.DXCanvas.StopRendering();

			shellView.BufferingStatus.Visibility = Visibility.Collapsed;
			
			UpdateFileRecentsMenuState();
			ShowStartupUI();

			if (!string.IsNullOrWhiteSpace(FileFromArgs))
			{
				LoggerManager.Info($"Opening URI from command line arguments: {FileFromArgs}");
				OpenURI(FileFromArgs);
			}

			osvrPlayback.OnGotFocus += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() =>
				{
					shellView.Activate();
				});
			});


			Logic.Instance.CheckForUpdate();
			Logic.Instance.CheckForBrowsers();
			Logic.Instance.stats.TrackScreen("Start screen");
			Logic.Instance.stats.TrackEvent("Application events", "Init", "Player launched");

			//LegacyTest();


			if (Logic.Instance.settings.EnableRemoteControl)
				EnableRemoteControl();
			if (Logic.Instance.settings.UseBlackBackground)
				shellView.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			if (Logic.Instance.settings.DisableUI)
			{
				shellView.controlBar.Visibility = Visibility.Collapsed;
				shellView.TopBar.Visibility = Visibility.Collapsed;
				shellView._OpenUrl.Visibility = Visibility.Collapsed;
				shellView._OpenFile.Visibility = Visibility.Collapsed;
			}

			Features.ListUpdated += LicenseUpdated;
			licensingConnector.LicenseCheck();

			InputDevices.NavigatorInputDevice.TryInit(windowHandle);
		}


		void LicenseUpdated()
		{
			if (!Features.Commercial)
				Logic.Notify("Please remember that 360Player requires a license for commercial use.");
			NotifyOfPropertyChange(null);
		}



		// Generic utility that receives events from the application and forwards them to whatever is interested
		public static void SendEvent(string name, object eventParameter = null)
		{
			RemoteControlSendEvent(name, eventParameter);
		}



		public void LoadMedia(bool autoplay = true)
		{
			_ready = false;
			if (!IsFileSelected) return;
			this.autoplay = autoplay;
			//if (!string.IsNullOrWhiteSpace(SelectedFileName))
			//{
			//	if (Path.GetFileNameWithoutExtension(SelectedFileName).ToLower().Contains("fbcube"))
			//		_mediaDecoder.Projection = MediaDecoder.ProjectionMode.CubeFacebook;
			//	else
			//		_mediaDecoder.Projection = MediaDecoder.ProjectionMode.Sphere;
			//}

			string mediaFile = SelectedFileName;
			Task.Factory.StartNew(() => _mediaDecoder.LoadMedia(mediaFile));
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			shellView = view as ShellView;
			DXCanvas = shellView.Canvas1;
			playerWindow = (view as Window);
			
			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;

			if (Logic.Instance.settings.StartInFullScreen)
				ToggleFullscreen(true);
			if (Logic.Instance.settings.AutoLoad)
				if (!string.IsNullOrWhiteSpace(Logic.Instance.settings.AutoPlayFile))
				{
					if (File.Exists(Logic.Instance.settings.AutoPlayFile))
					{
						SelectedFileName = Logic.Instance.settings.AutoPlayFile.Trim();
						LoadMedia();
						//Play();
					}
				}
			
			shellView.MouseMove += WatchUIVisibility;

			uiVisibilityBackgrundChecker = new BackgroundWorker();
			uiVisibilityBackgrundChecker.WorkerSupportsCancellation = true;
			uiVisibilityBackgrundChecker.DoWork += (sender, parameters) =>
			{
				while (!ended || uiVisibilityBackgrundChecker.CancellationPending)
				{

					if ((DateTime.Now - lastUIMove).TotalSeconds > 2)
					{
						if (IsPlaying)
						{
							if (uiVisible)
							{
								uiVisible = false;
								HideBars();
							}
						}
					}

					if ((DateTime.Now - lastCursorMove).TotalSeconds > 3)
					{
						if (Fullscreen)
							Execute.OnUIThread(() => Mouse.OverrideCursor = Cursors.None);
					}

					Thread.Sleep(100);
				}
			};

			uiVisibilityBackgrundChecker.RunWorkerAsync();
			
		}

		private DateTime lastUIMove;
		private DateTime lastCursorMove;
		private BackgroundWorker uiVisibilityBackgrundChecker;
		private bool uiVisible = true;

		public void WatchUIVisibility(object sender, MouseEventArgs e)
		{
			Execute.OnUIThread(() => Mouse.OverrideCursor = null);
			lastCursorMove = DateTime.Now;
			if (!IsPlaying || (IsPlaying && IsPaused))
			{
				lastUIMove = DateTime.Now;
				if (!uiVisible)
				{
					uiVisible = true;
					ShowBars();
				}
			}
			else
			if (IsPlaying && !IsPaused)
			{
				double height = shellView.ActualHeight;
				double Y = e.GetPosition(null).Y;
				if (!Fullscreen || (height - Y) < 120)
				{
					lastUIMove = DateTime.Now;
					if (!uiVisible)
					{
						uiVisible = true;
						ShowBars();
					}
				}
			}
		}



		protected override void OnDeactivate(bool close)
		{
			TryClose();
			base.OnDeactivate(close);
		}


		private double _timeValue = 0;
		public double TimeValue
		{
			get
			{
				return _timeValue;
			}

			set
			{
				if (!_mediaDecoder.IsPlaying) return;

				_timeValue = value;

				if (!lockSlider)
				{
					_mediaDecoder.Seek(value);
				}
				else
				{
					CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(_timeValue))).ToString();
					NotifyOfPropertyChange(() => CurrentPosition);
				}
			}
		}

		//private double _sphereSize = 6f;
		//public double SphereSize
		//{
		//	get { return _sphereSize; }
		//	set { this._sphereSize = value; /*BivrostPlayerPrototype.PlayerPrototype.SetSphereSize(value);*/ }
		//}


		private double _maxTime = double.MaxValue;
		public double MaxTime
		{
			get { return _maxTime; }
			set { _maxTime = value; NotifyOfPropertyChange(() => MaxTime); }
		}

		private void UpdateTimeLabel()
		{
			if (ended) return;
			VideoTime = CurrentPosition + " / " + VideoLength;
			NotifyOfPropertyChange(() => VideoTime);
		}


		public void Play()
		{
			//if (!IsFileSelected) return;

			//_mediaDecoder.LoadMedia(SelectedFileName);

			//Task.Factory.StartNew(() =>
			//{

			//waitForPlaybackReady.WaitOne();

			//if (_mediaDecoder.LastError != null) return;

			if (!_ready) return;

			Execute.OnUIThreadAsync(() =>
			{
				ShowPlaybackUI();
				TimeValue = 0;
				MaxTime = _mediaDecoder.Duration;
				VideoLength = (new TimeSpan(0, 0, (int)Math.Floor(_mediaDecoder.Duration))).ToString();
				UpdateTimeLabel();
				//DisplayName = DisplayString + " - " + SelectedFileNameLabel;

				NotifyOfPropertyChange(() => PlayerTitle);

				_mediaDecoder.SetVolume(VolumeRocker.Volume);
					//_mediaDecoder.Play();

					//STATS
					Logic.Instance.stats.TrackEvent("Application events", "Play", "");

				Execute.OnUIThread(() =>
				{
					shellView.TopBar.Visibility = Visibility.Visible;
					this.DXCanvas.Visibility = Visibility.Visible;
				});

				var scene = new Scene(_mediaDecoder.TextureL, _mediaDecoder.Projection);
				this.DXCanvas.Scene = scene;
                this.DXCanvas.StartRendering();

				HeadsetEnable += scene.HeadsetEnabled;
				HeadsetDisable += scene.HeadsetDisabled;



				Task.Factory.StartNew(() => ResetVR());

				_mediaDecoder.Play();

				shellView.PlayPause.Visibility = Visibility.Collapsed;
				shellView.Pause.Visibility = Visibility.Visible;
				NotifyOfPropertyChange(null);

				playerWindow.Focus();
				AnimateIndicator(shellView.PlayIndicator);
			});

			//});			
		}

        

		void ResetPlayback()
		{
			if (IsPlaying)
			{
				waitForPlaybackStop.Reset();
				Stop();
				waitForPlaybackStop.WaitOne();
			}
			else
			{
				int it = 5;
				while (_mediaDecoder.Initialized && it > 0)
				{
					Thread.Sleep(100);
					it--;
				}
			}

			_mediaDecoder.Projection = MediaDecoder.ProjectionMode.Sphere;
			_mediaDecoder.StereoMode = MediaDecoder.VideoMode.Autodetect;
			SelectedFileTitle = "";
		}


		/// <summary>
		/// Dialog opened from:
		/// - Open Url button in menu
		/// - Open Url button on center of screen
		/// </summary>
		public void OpenUrl()
		{
			Execute.OnUIThread(() => IsFileSelected = false);
			string uri = OpenUrlViewModel.GetURI();
			if(uri == null)
			{
				LoggerManager.Info("User cancelled OpenURI window.");
				return;
			}
			OpenURI(uri);
		}


		public void OpenURI(string uri)
		{
			SelectedFileTitle = "";

			Streaming.ServiceResult result = ServiceResultResolver.DialogProcessURIBlocking(uri, ShellViewModel.Instance.playerWindow);
			LoggerManager.Info($"OpenURI: Parsed '{uri}' to {result}");

			if (result == null)
			{
				urlLoadLock = false;
				return;
			}

			Execute.OnUIThread(() =>
			{
				ResetPlayback();

				SelectedFileName = result.BestQualityVideoStream(Streaming.VideoContainer.mp4).url;
                SelectedServiceResult = result;

				IsFileSelected = true;
				_mediaDecoder.Projection = result.projection;
				_mediaDecoder.StereoMode = result.stereoscopy;
				SelectedFileTitle = result.title;

				Recents.AddRecent(result);
				UpdateFileRecentsMenuState();

				LoadMedia();
			});
		}

		
        public void PlayPause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (!_ready)
				return;

			if (!IsPlaying)
			{
				if (CanPlay)
					LoadMedia();
				//Play();
			}
			else
			{
				if (IsPaused)
					UnPause();
				else Pause();
			}

		}

		public void Pause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			IsPaused = true;
			Task.Factory.StartNew(() =>
			{
				_mediaDecoder.Pause();
				ShellViewModel.SendEvent("moviePaused", Path.GetFileName(SelectedFileName));
			});
			Execute.OnUIThreadAsync(() =>
			{
				shellView.PlayPause.Visibility = Visibility.Visible;
				shellView.Pause.Visibility = Visibility.Collapsed;
				NotifyOfPropertyChange(() => CanPlay);
				AnimateIndicator(shellView.PauseIndicator);
			});

			VRUIPause();
		}

		public void UnPause()
		{
			Execute.OnUIThreadAsync(() =>
			{
				IsPaused = false;
				Task.Factory.StartNew(() =>
				{
					_mediaDecoder.Unpause();
					ShellViewModel.SendEvent("movieUnpaused", Path.GetFileName(SelectedFileName));
				});
				shellView.PlayPause.Visibility = Visibility.Collapsed;
				shellView.Pause.Visibility = Visibility.Visible;
				NotifyOfPropertyChange(() => CanPlay);
				AnimateIndicator(shellView.PlayIndicator);

				VRUIUnpause();
			});
		}


		/// <summary>
		/// Open/Choose File dialog invoked from:
		/// - Menu Open File
		/// - Open file button
		/// </summary>
		public void OpenFile()
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			//ofd.Filter = "Video MP4|*.mp4|Video M4V|*.m4v|All|*.*";
			ofd.Filter = MediaDecoder.ExtensionsFilter();
			bool? result = ofd.ShowDialog();
			if (result.GetValueOrDefault(false))
				OpenURI(ofd.FileName);
		}


		#region recents

		/// <summary>
		/// Updates the File->(recent files) menu, binding the action to it and
		/// pruning to at most 10 entries
		/// </summary>
		public void UpdateFileRecentsMenuState()
		{
			//Recents.UpdateMenu(shellView.FileMenuItem, OpenURI);
		}


		

		


		public List<RecentsItem> Items { get { return Recents.RecentFiles.ConvertAll(r => new RecentsItem(r)); } }
		



		#endregion


		public void OpenAbout()
		{
			DialogHelper.ShowDialog<AboutViewModel>();
		}


		public void OpenLogViewer()
		{
			Bivrost.Log.LogWindow.OpenIfClosed();
		}

		public void FileDropped(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				// Note that you can have more than one file.
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				string ext = Path.GetExtension(files[0]);
				//if (Path.GetExtension(files[0]) == ".mp4")
				if (MediaDecoder.CheckExtension(Path.GetExtension(files[0])))
				{
					OpenURI(files[0]);
				}
				else
				{
					NotificationCenter.PushNotification(new NotificationViewModel("File format not supported."));
				}
			}
			ShowDropFilesPanel(false);
			if (IsFileSelected == false) ShowStartupPanel(true);
			PlaybackControlUIHitTestVisible(true);
		}

		public void SliderLock()
		{
			lockSlider = true;
		}

		public void SliderUnlock()
		{
			lockSlider = false;
			_mediaDecoder.Seek(_timeValue);
		}

		public void FilePreviewDragEnter(DragEventArgs e)
		{
			ShowDropFilesPanel(true);
			ShowStartupPanel(false);
			PlaybackControlUIHitTestVisible(false);
			e.Handled = true;
		}

		public void FilePreviewDragLeave(DragEventArgs e)
		{
			ShowDropFilesPanel(false);
			if (IsFileSelected == false) ShowStartupPanel(true);
			PlaybackControlUIHitTestVisible(true);
			e.Handled = true;
		}

		public bool CanPlay { get { return (!IsPlaying || IsPaused) && IsFileSelected; } }
		public bool CanStopOrRewind { get { return IsPlaying; } }

		//public bool CanOpenFile { get { return !IsPlaying; } }


		public void Stop()
		{
			LoggerManager.Info("FILE ENDED");
			if (Fullscreen) if (!Logic.Instance.settings.DoNotExitFullscreenOnStop) ToggleFullscreen(true);
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());
			ShowBars();
			ShowStartupUI();

			LoggerManager.Info("STOP STOP STOP");

			this.DXCanvas.Scene = null;

			headsets.ForEach(h => h.Stop());

			Execute.OnUIThread(() =>
			{
				shellView.TopBar.Visibility = Visibility.Hidden;
				this.DXCanvas.Visibility = Visibility.Hidden;
			});

			Task.Factory.StartNew(() =>
			{
				if (IsPlaying || _mediaDecoder.IsEnded)
				{
					_mediaDecoder.Stop();
					ShellViewModel.SendEvent("movieStopped", Path.GetFileName(SelectedFileName));
					//STATS
					Logic.Instance.stats.TrackEvent("Application events", "Stop", "");

					_timeValue = 0;
					try
					{
						Execute.OnUIThread(() =>
						{
							NotifyOfPropertyChange(() => TimeValue);
							NotifyOfPropertyChange(() => CanPlay);
							CurrentPosition = (new TimeSpan(0, 0, 0)).ToString();
							UpdateTimeLabel();

							shellView.PlayPause.Visibility = Visibility.Visible;
							shellView.Pause.Visibility = Visibility.Collapsed;

							this.DXCanvas.StopRendering();
						});
					}
					catch (Exception) { }
				}
			});

			waitForPlaybackStop.Set();

			NotifyOfPropertyChange(() => PlayerTitle);
			NotifyOfPropertyChange(() => CanStopOrRewind);
		}

		public override void TryClose(bool? dialogResult = null)
		{
			ended = true;
			Stop();
			Task.Factory.StartNew(async () =>
			{
				await Logic.Instance.stats.TrackQuit();
			});

			base.TryClose(dialogResult);

			if (_mediaDecoder != null)
				_mediaDecoder.Shutdown();
			//nancy.Stop();

		}

		public void Rewind()
		{
			if (_mediaDecoder != null)
			{
				_mediaDecoder.Seek(0);
				if (_mediaDecoder.IsEnded || _mediaDecoder.IsPaused)
					PlayPause();
			}
			//BivrostPlayerPrototype.PlayerPrototype.Rewind();
		}

		public void Quit()
		{
			if (remoteControl != null)
			{
				remoteControl.Stop();
			}
			if (IsRemoteControlEnabled)
			{
				ShellViewModel.SendEvent("quit");
			}

			//STATS
			TryClose();
		}

		public void OpenSettings()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			DialogHelper.ShowDialog<ConfigurationViewModel>();
			Logic.Instance.ValidateSettings();

			// update all settings after exiting settings dialog
			NotifyOfPropertyChange(null);	
		}


		#region mouse events
		private Point _mouseDownPoint;
		private bool _drag = false;
		private IInputElement _element;
		private Point _dragLastPosition;
		private DateTime _doubleClickFirst;
		private bool _doubleClickDetected = false;
		private bool _waitingForDoubleClickTimeout = false;
		private Point _waitingPoint;

		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (_doubleClickDetected) return;
			var current = e.GetPosition(null);
			var delta = current - _dragLastPosition;
			_dragLastPosition = current;
			if (this.DXCanvas.Scene != null)
			{
				((Scene)this.DXCanvas.Scene).MoveDelta((float)delta.X, (float)delta.Y, (float)(72f / this.DXCanvas.ActualWidth) * 1.5f, 20f);
			}
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (_doubleClickDetected) _doubleClickDetected = false;
			else
			if ((DateTime.Now - _doubleClickFirst).TotalMilliseconds < 250)
			{
				ToggleFullscreen(true);
				_doubleClickDetected = true;
			}
			_doubleClickFirst = DateTime.Now;

			_mouseDownPoint = e.GetPosition(null);
			_dragLastPosition = _mouseDownPoint;
			_drag = false;
			_element = (IInputElement)e.Source;
			_element.CaptureMouse();
			_element.MouseMove += MouseMove;
		}

		public void MouseUp(MouseButtonEventArgs e)
		{
			if (!_waitingForDoubleClickTimeout)
			{
				_waitingPoint = e.GetPosition(null);
				_waitingForDoubleClickTimeout = true;
				Task.Factory.StartNew(() =>
				{
					Thread.Sleep(280);
					Execute.OnUIThread(() =>
				   {
					   if (!_doubleClickDetected)
					   {
						   if (_mouseDownPoint == _waitingPoint && !_drag)
						   {
							   PlayPause();
						   }
						   else
						   {

						   }
					   }
					   _waitingForDoubleClickTimeout = false;
				   });
				});
			}

			if (_element != null)
			{
				_element.ReleaseMouseCapture();
				_element.MouseMove -= MouseMove;
			}
		}
		#endregion


		#region fullscreen
		private bool _fullscreen = false;
		public bool Fullscreen
		{
			get { return this._fullscreen; }
			set { this._fullscreen = value; NotifyOfPropertyChange(() => Fullscreen); }
		}

		//public void ToggleFullscreen() { ToggleFullscreen(false); }

		public void ToggleFullscreen(bool realToggle = false)
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (realToggle)
				Fullscreen = !Fullscreen;

			if (!Fullscreen)
			{
				Mouse.OverrideCursor = null;
				ShowUI();
				playerWindow.WindowState = WindowState.Normal;
				playerWindow.WindowStyle = WindowStyle.SingleBorderWindow;
				//playerWindow.Topmost = false;
				playerWindow.ResizeMode = ResizeMode.CanResize;
			}
			else
			{
				Mouse.OverrideCursor = null;
				HideUI();
				playerWindow.WindowState = WindowState.Normal;
				playerWindow.WindowStyle = WindowStyle.None;
				//playerWindow.Topmost = true;
				playerWindow.ResizeMode = ResizeMode.NoResize;
				playerWindow.Margin = new Thickness(0, 0, 0, 0);
				playerWindow.WindowState = WindowState.Maximized;
			}

			ShellViewModel.SendEvent("fullscreenChanged", Fullscreen);
		}

		public void EscapeFullscreen()
		{
			if (Fullscreen) ToggleFullscreen(true);
		}
		#endregion



		public void OnLostFocus()
		{
			if (IsPlaying)
			{
				if (this.DXCanvas.Scene != null)
					((Scene)this.DXCanvas.Scene).HasFocus = false;
			}
		}

		public void OnGotFocus()
		{
			if (IsPlaying)
			{
                if (this.DXCanvas.Scene != null)
                {
                    ((Scene)this.DXCanvas.Scene).HasFocus = true;
                    this.shellView.PlayPause.Focus();
                }
			}
		}

		public void ShowVolumeControl()
		{
			VolumeRocker.Show();
		}

		public void Mute()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			VolumeRocker.ToggleMute();
			NotifyOfPropertyChange(() => VolumeTooltip);
		}

		public string VolumeTooltip
		{
			get { return "Volume: " + (VolumeRocker.IsMuted ? "muted" : (Math.Round(VolumeRocker.Volume * 100) + "%")); }
		}

        public static event Action<ShellViewModel> OnInstantiated;

        public void VolumeMouseWheel(MouseWheelEventArgs e)
		{
			VolumeRocker.MouseWheel(e);
			NotifyOfPropertyChange(() => VolumeTooltip);
		}

		public void FastForward()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (IsPlaying)
			{
				_mediaDecoder.Seek(_mediaDecoder.CurrentPosition + 5f);
			}
		}

		public void FastRewind()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (IsPlaying)
			{
				_mediaDecoder.Seek(_mediaDecoder.CurrentPosition - 5f);
			}
		}

		public void HeadsetSelect()
		{
			HeadsetMenu.ToggleVisibility();
		}


 
		#region menu options: projection
		protected void SetProjection(MediaDecoder.ProjectionMode? projection)
		{
			HACK_Projection = projection;

			if (DXCanvas.Scene != null)
			{
				Scene scene = (Scene)DXCanvas.Scene;
				scene.UpdateSceneSettings(projection.GetValueOrDefault(MediaDecoder.ProjectionMode.Sphere), MediaDecoder.VideoMode.Autodetect);
			}

			UpdateVRSceneSettings(
				projection.GetValueOrDefault(MediaDecoder.ProjectionMode.Sphere), 
				MediaDecoder.VideoMode.Autodetect
			);

			NotifyOfPropertyChange(() => ProjectionIsAuto);
			NotifyOfPropertyChange(() => ProjectionIsEquirectangular);
			NotifyOfPropertyChange(() => ProjectionIsCubeFacebook);
			NotifyOfPropertyChange(() => ProjectionIsDome);
		}

		protected MediaDecoder.ProjectionMode? HACK_Projection = null;
		public bool ProjectionIsAuto
		{
			get { return !HACK_Projection.HasValue; }
			set { if (value) SetProjection(null); }
		}
		public bool ProjectionIsEquirectangular
		{
			get { return HACK_Projection.HasValue && HACK_Projection == MediaDecoder.ProjectionMode.Sphere; }
			set { if (value) SetProjection(MediaDecoder.ProjectionMode.Sphere); }
		}
		public bool ProjectionIsCubeFacebook
		{
			get { return HACK_Projection.HasValue && HACK_Projection == MediaDecoder.ProjectionMode.CubeFacebook; }
			set { if (value) SetProjection(MediaDecoder.ProjectionMode.CubeFacebook); }
		}
		public bool ProjectionIsDome
		{
			get { return HACK_Projection.HasValue && HACK_Projection == MediaDecoder.ProjectionMode.Dome; }
			set { if (value) SetProjection(MediaDecoder.ProjectionMode.Dome); }
		}
		#endregion



		#region menu options: analitics



		public AnaliticsMenuViewModel AnaliticsMenu { get; } = new AnaliticsMenuViewModel();
		#endregion

	}

	#region recents menu helpers
	public class ObjectToTypeConverter : System.Windows.Data.IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;
			else
				return value.GetType();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public class RecentsItem
	{
		private Recents.RecentsFormat2.RecentElement recent;

		public RecentsItem(Recents.RecentsFormat2.RecentElement recentElement)
		{
			this.recent = recentElement;
		}

		public class RunRecentItemCommand : ICommand
		{
			private RecentsItem recentsItem;

			public RunRecentItemCommand(RecentsItem recentsItem)
			{
				this.recentsItem = recentsItem;
			}

			public event EventHandler CanExecuteChanged;

			public bool CanExecute(object parameter)
			{
				return true;
			}

			public void Execute(object parameter)
			{
				ShellViewModel.Instance.OpenURI(recentsItem.recent.uri);
			}
		}


		public string Header
		{
			get { return recent.title; }
		}
		public ICommand Command
		{
			get { return new RunRecentItemCommand(this); }
		}

	}
	#endregion

}
