﻿using Caliburn.Micro;
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
using Bivrost.Log;
using Bivrost.MOTD;
using Bivrost.Bivrost360Player.Streaming;

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

		public string SelectedFileNameLabel => SelectedServiceResult?.TitleWithFallback ?? "";


		public string SelectedFileName => SelectedServiceResult?.BestSupportedStream;

		private ServiceResult _selectedServiceResult = null;
		public ServiceResult SelectedServiceResult
        {
			get { return _selectedServiceResult; }
			set
			{
				_selectedServiceResult = value;
				NotifyOfPropertyChange(nameof(SelectedFileName));
				NotifyOfPropertyChange(nameof(SelectedFileTitle));
				NotifyOfPropertyChange(nameof(SelectedFileDescription));
				NotifyOfPropertyChange(nameof(SelectedFileNameLabel));
				NotifyOfPropertyChange(nameof(IsContentAVideo));
				NotifyOfPropertyChange(nameof(ShouldShowVideoTime));
			}
		}

		public bool IsContentAVideo => (SelectedServiceResult == null) 
			? true 
			: SelectedServiceResult.contentType == Streaming.ServiceResult.ContentType.video;

		public bool ShouldShowVideoTime => (SelectedServiceResult == null)
			? false
			: SelectedServiceResult.contentType == Streaming.ServiceResult.ContentType.video && IsPlaying;

		public bool IsFileSelected { get; set; }

		public string SelectedFileTitle => SelectedServiceResult?.title;
		public string SelectedFileDescription { get; } = "";

		public DPFCanvas DXCanvas;
		public ShellView shellView;

		private bool ended = false;
		private bool lockSlider = false;

		private bool autoplay = false;
		private MediaDecoder _mediaDecoder;
		private bool _ready = false;

		Window playerWindow;
		private AutoResetEvent waitForPlaybackReady = new AutoResetEvent(false);
		private ManualResetEvent waitForPlaybackStop = new ManualResetEvent(false);

		public string PlayerTitle {
			get
			{
				string title = "BIVROST® 360Player ™ ";
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

#if FEATURE_LICENSE_NINJA
		Bivrost.Licensing.LicensingConnector licensingConnector = 
			new Bivrost.Licensing.LicensingConnector(new LicensingContext());
#endif

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
						"A new version of Bivrost® 360Player is available.",
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
			
			ShowStartupUI();

			if (!string.IsNullOrWhiteSpace(FileFromArgs))
			{
				LoggerManager.Info($"Opening URI from command line arguments: {FileFromArgs}");
				OpenURI(FileFromArgs);
			}

			Logic.Instance.CheckForUpdate();
#if FEATURE_BROWSER_PLUGINS
			BrowserPluginManagement.CheckForBrowsers();
#endif
			Logic.Instance.stats.TrackScreen("Start screen");
			Logic.Instance.stats.TrackEvent("Application events", "Init", "Player launched");

			//LegacyTest();

#if FEATURE_REMOTE_CONTROL
			if (Logic.Instance.settings.EnableRemoteControl)
				EnableRemoteControl();
#endif
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
#if FEATURE_LICENSE_NINJA
			licensingConnector.LicenseCheck();
#else
			// set hardcoded basic no-license-required features
			Features.SetBasicFeatures();
#endif

			InputDevices.NavigatorInputDevice.TryInit(windowHandle);

			var motdBridge = new MOTDBridge();
			motd = new MOTDClient("https://download.bivrost360.com/player-desktop/?action=", motdBridge);

			var currentVersion = motdBridge.Version;
			var prevVersion = Logic.Instance.settings.LastStoredPlayerVersion;
			if (currentVersion != prevVersion) //< first install or an update?
			{
				motd.RequestUpgradeNotice(prevVersion);
				Logic.Instance.settings.LastStoredPlayerVersion = currentVersion;
				Logic.Instance.settings.Save();
			}
			else
			{
				motd.RequestMOTD();
			}
		}


		MOTDClient motd;


		void LicenseUpdated()
		{
			if (!Features.Commercial)
				Logic.Notify("Please remember that 360Player requires a license for commercial use.");
			NotifyOfPropertyChange(null);
		}



		// Generic utility that receives events from the application and forwards them to whatever is interested
		public static void SendEvent(string name, object eventParameter = null)
		{
#if FEATURE_REMOTE_CONTROL
			RemoteControlSendEvent(name, eventParameter);
#endif
		}



		public void LoadMedia(ServiceResult result, bool autoplay = true)
		{

			SelectedServiceResult = result;

			IsFileSelected = true;

			Recents.AddRecent(result);
			NotifyOfPropertyChange(nameof(Items));

			_ready = false;
			if (!IsFileSelected) return;
			this.autoplay = autoplay;

			if (CurrentHeadset != null)
			{
				CurrentHeadset.Media = SelectedServiceResult;
			}

			Task.Factory.StartNew(() => _mediaDecoder.LoadMedia(result));
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
						//SelectedFileName = Logic.Instance.settings.AutoPlayFile.Trim();

						var result = StreamingFactory.Instance.GetStreamingInfo(Logic.Instance.settings.AutoPlayFile.Trim());

						LoadMedia(result);
						//Play();
					}
				}

			//ResetVR();


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

				var scene = new Scene(_mediaDecoder.ContentRequested);
				this.DXCanvas.Scene = scene;
                this.DXCanvas.StartRendering();

				HeadsetEnable += scene.HeadsetEnabled;
				HeadsetDisable += scene.HeadsetDisabled;
				if (CurrentHeadset != null)
					scene.HeadsetEnabled(CurrentHeadset);



				Task.Factory.StartNew(() => ResetVR());

				_mediaDecoder.Play();

				shellView.PlayPause.Visibility = Visibility.Collapsed;
				shellView.Pause.Visibility = Visibility.Visible;
				NotifyOfPropertyChange(null);

				playerWindow.Focus();
				if(IsContentAVideo)
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

			//_mediaDecoder.Projection = ProjectionMode.Sphere;
			//_mediaDecoder.StereoMode = VideoMode.Autodetect;

			SelectedServiceResult = null;
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
			SelectedServiceResult = null;

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

				LoadMedia(result);
			});
		}

		
        public void PlayPause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (!_ready)
				return;

			if (!IsPlaying)		// <press space or click while movie disabled hack
			{
				if (CanPlay)
					LoadMedia(SelectedServiceResult);
				// is automatic - Play();
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
				if (IsContentAVideo)
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
				if (IsContentAVideo)
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
		public bool CanStopOrRewind { get { return IsPlaying && !_mediaDecoder.IsDisplayingStaticContent; } }

		//public bool CanOpenFile { get { return !IsPlaying; } }


		public void Stop()
		{
			LoggerManager.Info("File ended");
			if (Fullscreen) if (!Logic.Instance.settings.DoNotExitFullscreenOnStop) ToggleFullscreen(true);
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());
			ShowBars();
			ShowStartupUI();

			LoggerManager.Info("Media stopped");

			this.DXCanvas.Scene = null;

			HeadsetStop();
			//headsets.ForEach(h => h.Stop());

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

			//SelectedServiceResult = null;

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
#if FEATURE_REMOTE_CONTROL
			remoteControl?.Stop();
#endif
			SendEvent("quit");

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
		protected void SetProjection(ProjectionMode projection)
		{
			_mediaDecoder.Projection = projection;

			//if (DXCanvas.Scene != null)
			//{
			//	Scene scene = (Scene)DXCanvas.Scene;
			//	scene.UpdateSceneSettings(projection, VideoMode.Autodetect);
			//}

			//UpdateVRSceneSettings(
			//	projection.GetValueOrDefault(ProjectionMode.Sphere), 
			//	VideoMode.Autodetect
			//);



			//void UpdateVRSceneSettings(ProjectionMode projectionMode, VideoMode videoMode)
			//{
			//	CurrentHeadset?.UpdateSceneSettings(projectionMode, videoMode);
			//}

			NotifyOfPropertyChange(() => ProjectionIsAuto);
			NotifyOfPropertyChange(() => ProjectionIsEquirectangular);
			NotifyOfPropertyChange(() => ProjectionIsCubeFacebook);
			NotifyOfPropertyChange(() => ProjectionIsDome);
		}

		public bool ProjectionIsAuto
		{
			get { return _mediaDecoder.Projection == ProjectionMode.Autodetect; }
			set { if (value) SetProjection(ProjectionMode.Autodetect); }
		}
		public bool ProjectionIsEquirectangular
		{
			get { return _mediaDecoder.Projection == ProjectionMode.Sphere; }
			set { if (value) SetProjection(ProjectionMode.Sphere); }
		}
		public bool ProjectionIsCubeFacebook
		{
			get { return _mediaDecoder.Projection == ProjectionMode.CubeFacebook; }
			set { if (value) SetProjection(ProjectionMode.CubeFacebook); }
		}
		public bool ProjectionIsDome
		{
			get { return _mediaDecoder.Projection == ProjectionMode.Dome; }
			set { if (value) SetProjection(ProjectionMode.Dome); }
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
			get {
				switch(Path.GetExtension(recent.uri).ToLowerInvariant())
				{
					//case ".jpg":
					//case ".jpeg":
					//case ".png":
					//	return recent.title + " 📷"; //  (🖼)
					default:
						return recent.title;
				}
			}
		}
		public ICommand Command
		{
			get { return new RunRecentItemCommand(this); }
		}

	}
#endregion

}
